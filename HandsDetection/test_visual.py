import cv2
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision

# ── Detector ─────────────────────────────────────────────────────────────
base_options = mp_python.BaseOptions(model_asset_path='hand_landmarker.task')
options = vision.HandLandmarkerOptions(
    base_options=base_options,
    num_hands=2,
    min_hand_detection_confidence=0.7,
    min_tracking_confidence=0.5,
    running_mode=vision.RunningMode.VIDEO
)
detector = vision.HandLandmarker.create_from_options(options)

CONNECTIONS = [
    (0,1),(1,2),(2,3),(3,4),
    (0,5),(5,6),(6,7),(7,8),
    (5,9),(9,10),(10,11),(11,12),
    (9,13),(13,14),(14,15),(15,16),
    (13,17),(17,18),(18,19),(19,20),
    (0,17)
]

video = cv2.VideoCapture(0)
timestamp_ms = 0

while True:
    success, img = video.read()
    if not success:
        break
    img = cv2.flip(img, 1)
    h, w, _ = img.shape
    rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
    result = detector.detect_for_video(mp_image, timestamp_ms)
    timestamp_ms += 33

    total = 0

    if result.hand_landmarks:
        for landmarks, handedness in zip(result.hand_landmarks, result.handedness):
            label = handedness[0].category_name  # "Left" o "Right"

            pontos = [(int((1 - lm.x) * w), int(lm.y * h)) for lm in landmarks]

            # Dibujar esqueleto
            for a, b in CONNECTIONS:
                cv2.line(img, pontos[a], pontos[b], (200, 200, 200), 2)
            for x, y in pontos:
                cv2.circle(img, (x, y), 5, (0, 200, 100), -1)

            dedos = [8, 12, 16, 20]
            contador = 0

            # Pulgar: dirección opuesta según la mano
            # Al hacer flip, Left y Right se intercambian visualmente
            if label == "Right":
                if pontos[4][0] < pontos[3][0]:   # mano derecha — pulgar va a la izquierda
                    contador += 1
            else:
                if pontos[4][0] > pontos[3][0]:   # mano izquierda — pulgar va a la derecha
                    contador += 1

            for x in dedos:
                if pontos[x][1] < pontos[x - 2][1]:
                    contador += 1

            total += contador

            wx, wy = pontos[0]
            cv2.putText(img, str(contador),
                        (wx - 15, wy - 20),
                        cv2.FONT_HERSHEY_SIMPLEX, 1.5, (0, 255, 100), 3)

    # Caja azul con total
    cv2.rectangle(img, (80, 10), (220, 110), (255, 0, 0), -1)
    cv2.putText(img, "TOTAL", (88, 28),
                cv2.FONT_HERSHEY_SIMPLEX, 0.6, (200, 200, 255), 1)
    cv2.putText(img, str(total),
                (100, 100),
                cv2.FONT_HERSHEY_SIMPLEX, 4, (255, 255, 255), 5)

    cv2.imshow('Deteccion de dedos', img)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

video.release()
cv2.destroyAllWindows()