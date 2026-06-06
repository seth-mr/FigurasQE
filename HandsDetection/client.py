import os
os.environ['GLOG_minloglevel'] = '3'
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

import argparse
import queue
import threading
import time
import uuid

import cv2
import grpc
import fingers_pb2
import fingers_pb2_grpc

# ID unico por cliente
CLIENT_ID = str(uuid.uuid4())[:8]

# Estado compartido entre hilo de envio y hilo de recepcion
estado = {
    "left": 0,
    "right": 0,
    "total": 0,
    "hands": False,
    "connected": True,
    "error": None
}
estado_lock = threading.Lock()


def generar_frames(frame_queue):
    while True:
        item = frame_queue.get()

        if item is None:
            return

        ok, buffer = cv2.imencode('.jpg', item, [cv2.IMWRITE_JPEG_QUALITY, 80])
        if not ok:
            continue

        yield fingers_pb2.FrameRequest(
            image_data=buffer.tobytes(),
            client_id=CLIENT_ID
        )


def recibir_respuestas(responses):
    """Hilo separado para recibir respuestas del servidor."""
    try:
        for response in responses:
            with estado_lock:
                estado["left"] = response.left_hand
                estado["right"] = response.right_hand
                estado["total"] = response.total
                estado["hands"] = response.hands_detected
    except grpc.RpcError as error:
        with estado_lock:
            estado["connected"] = False
            estado["error"] = f"{error.code().name}: {error.details()}"
    finally:
        with estado_lock:
            estado["connected"] = False


def parse_args():
    parser = argparse.ArgumentParser(description="Cliente gRPC para deteccion de dedos.")
    parser.add_argument("--target", default="localhost:50051", help="Servidor gRPC host:puerto")
    parser.add_argument("--camera", type=int, default=0, help="Indice de camara de OpenCV")
    return parser.parse_args()


def main():
    args = parse_args()

    channel = grpc.insecure_channel(
        args.target,
        options=[
            ('grpc.max_receive_message_length', 10 * 1024 * 1024),
            ('grpc.max_send_message_length', 10 * 1024 * 1024),
        ]
    )
    stub = fingers_pb2_grpc.FingerDetectorStub(channel)

    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        print(f"No se pudo abrir la camara {args.camera}. Prueba con --camera 1 o revisa permisos.")
        channel.close()
        return

    frame_queue = queue.Queue(maxsize=2)
    responses = stub.StreamFrames(generar_frames(frame_queue))
    hilo = threading.Thread(target=recibir_respuestas, args=(responses,), daemon=True)
    hilo.start()

    print(f"Cliente {CLIENT_ID} conectado a {args.target}")
    print("Presiona q para salir.")

    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                print("La camara dejo de entregar frames; cerrando cliente.")
                break

            # Enviar el mismo frame que se muestra. Si la cola esta llena, descartamos el mas viejo.
            try:
                frame_queue.put_nowait(frame.copy())
            except queue.Full:
                try:
                    frame_queue.get_nowait()
                except queue.Empty:
                    pass
                frame_queue.put_nowait(frame.copy())

            frame = cv2.flip(frame, 1)
            h, w, _ = frame.shape

            with estado_lock:
                left = estado["left"]
                right = estado["right"]
                total = estado["total"]
                hands = estado["hands"]
                connected = estado["connected"]
                error = estado["error"]

            cv2.rectangle(frame, (80, 10), (220, 110), (255, 0, 0), -1)
            cv2.putText(frame, "TOTAL", (88, 28),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.6, (200, 200, 255), 1)
            cv2.putText(frame, str(total),
                        (100, 100),
                        cv2.FONT_HERSHEY_SIMPLEX, 4, (255, 255, 255), 5)

            if hands:
                cv2.putText(frame, f"IZQ: {left}",
                            (10, h - 60),
                            cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 100), 2)
                cv2.putText(frame, f"DER: {right}",
                            (10, h - 20),
                            cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 100), 2)

            status = "conectado" if connected else "desconectado"
            cv2.putText(frame, f"ID: {CLIENT_ID} | {status}",
                        (10, 30),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.6, (200, 200, 200), 1)

            if error:
                cv2.putText(frame, error[:70],
                            (10, 58),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.55, (0, 0, 255), 2)

            cv2.imshow(f'Cliente {CLIENT_ID}', frame)

            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

            if not connected and error:
                print(f"El servidor cerro el stream: {error}")
                time.sleep(1)
                break
    finally:
        frame_queue.put(None)
        cap.release()
        cv2.destroyAllWindows()
        channel.close()


if __name__ == '__main__':
    main()
