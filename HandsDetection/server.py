import os
os.environ['GLOG_minloglevel'] = '3'
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

import grpc
import cv2
import numpy as np
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision
from concurrent import futures
import threading
import fingers_pb2
import fingers_pb2_grpc

# Importar el health server HTTP
from health_server import start_health_server

CONNECTIONS = [
    (0,1),(1,2),(2,3),(3,4),
    (0,5),(5,6),(6,7),(7,8),
    (5,9),(9,10),(10,11),(11,12),
    (9,13),(13,14),(14,15),(15,16),
    (13,17),(17,18),(18,19),(19,20),
    (0,17)
]

def crear_detector():
    """Cada hilo necesita su propio detector"""
    base_options = mp_python.BaseOptions(model_asset_path='hand_landmarker.task')
    options = vision.HandLandmarkerOptions(
        base_options=base_options,
        num_hands=2,
        min_hand_detection_confidence=0.7,
        min_tracking_confidence=0.5,
        running_mode=vision.RunningMode.VIDEO
    )
    return vision.HandLandmarker.create_from_options(options)

def contar_dedos(landmarks, handedness, w, h):
    label = handedness[0].category_name
    pontos = [(int((1 - lm.x) * w), int(lm.y * h)) for lm in landmarks]

    dedos = [8, 12, 16, 20]
    contador = 0

    if label == "Right":
        if pontos[4][0] < pontos[3][0]:
            contador += 1
    else:
        if pontos[4][0] > pontos[3][0]:
            contador += 1

    for x in dedos:
        if pontos[x][1] < pontos[x - 2][1]:
            contador += 1

    return label, contador

class FingerDetectorServicer(fingers_pb2_grpc.FingerDetectorServicer):
    def __init__(self):
        self.detector = crear_detector()
        self.detector_lock = threading.Lock()
        self.timestamp_ms = 0

    def StreamFrames(self, request_iterator, context):
        client_id = "unknown"
        print(f"[+] Cliente conectado")

        try:
            for request in request_iterator:
                client_id = request.client_id

                # Decodificar JPEG
                nparr = np.frombuffer(request.image_data, np.uint8)
                frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
                if frame is None:
                    continue

                h, w, _ = frame.shape
                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)

                with self.detector_lock:
                    result = self.detector.detect_for_video(mp_image, self.timestamp_ms)
                    self.timestamp_ms += 33

                left_count  = 0
                right_count = 0
                hands_detected = False

                if result.hand_landmarks:
                    hands_detected = True
                    for landmarks, handedness in zip(result.hand_landmarks, result.handedness):
                        label, count = contar_dedos(landmarks, handedness, w, h)
                        if label == "Left":
                            left_count = count
                        else:
                            right_count = count

                yield fingers_pb2.FingerResponse(
                    left_hand=left_count,
                    right_hand=right_count,
                    total=left_count + right_count,
                    hands_detected=hands_detected,
                    client_id=client_id
                )

        except Exception as e:
            print(f"[-] Cliente {client_id} desconectado: {e}")

        print(f"[-] Cliente {client_id} terminó")

def serve():
    # Iniciar health server HTTP en background
    start_health_server(port=8080)

    server = grpc.server(
        futures.ThreadPoolExecutor(max_workers=10),  # hasta 10 clientes simultáneos
        options=[
            ('grpc.max_receive_message_length', 10 * 1024 * 1024),  # 10MB por frame
            ('grpc.max_send_message_length',    10 * 1024 * 1024),
        ]
    )
    fingers_pb2_grpc.add_FingerDetectorServicer_to_server(
        FingerDetectorServicer(), server
    )
    server.add_insecure_port('[::]:50051')
    server.start()
    print("Servidor escuchando en puerto 50051 — esperando clientes...")
    server.wait_for_termination()

if __name__ == '__main__':
    serve()
