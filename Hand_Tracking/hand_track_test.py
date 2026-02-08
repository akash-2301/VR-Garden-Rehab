# ORIGINAL CODE
# send_hand_position_for_calibration.py
import socket
import json
import cv2
import mediapipe as mp  
import pyrealsense2 as rs
import numpy as np

# === Setup RealSense + Mediapipe ===
pipeline = rs.pipeline()
config = rs.config()
config.enable_stream(rs.stream.color, 1280, 720, rs.format.bgr8, 30)
pipeline.start(config)

mp_hands = mp.solutions.hands.Hands(max_num_hands=1)    
mp_draw = mp.solutions.drawing_utils  # for drawing landmarks
    
hand_x, hand_y = 0, 0
hand_detected = False

# === Socket Server ===
HOST = '127.0.0.1'
PORT = 5010
server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.bind((HOST, PORT))
server.listen(1)
print(f"[SERVER] Waiting for Unity on {HOST}:{PORT}")
conn, addr = server.accept()
print(f"[SERVER] Connected by {addr}")

try:
    while True:
        frames = pipeline.wait_for_frames()
        color_frame = frames.get_color_frame()
        if not color_frame:
            continue
        img = np.asanyarray(color_frame.get_data())
        img = cv2.flip(img, 1)  # Flip to get correct (non-mirrored) real-world view
        img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)


        results = mp_hands.process(img_rgb)

        if results.multi_hand_landmarks:
            hand_detected = True
            hand_landmarks = results.multi_hand_landmarks[0]

            # Get the landmark #9 (middle of palm)
            wrist = hand_landmarks.landmark[9]
            hand_x = wrist.x * 1280
            hand_y = wrist.y * 720

            # Draw all landmarks
            mp_draw.draw_landmarks(img, hand_landmarks, mp.solutions.hands.HAND_CONNECTIONS)

            # Draw green dot on landmark 9
            cv2.circle(img, (int(hand_x), int(hand_y)), 10, (0, 255, 0), -1)

            # Draw coordinates beside the dot
            text = f"({int(hand_x)}, {int(hand_y)})"
            cv2.putText(img, text, (int(hand_x) + 15, int(hand_y) - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)
        else:
            hand_detected=False

        # Show camera image with overlay
        cv2.imshow("Hand Tracking with Green Dot", img)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

        # Wait for Unity to ask for data
        data = conn.recv(1024).decode()
        if not data:
            print("Unity disconnected.")
            break

        if data == "get":
            payload = json.dumps({ "x": hand_x, "y": hand_y,"handDetected": hand_detected})
            print("[SERVER] Sending:", payload)

            conn.sendall(payload.encode())

except KeyboardInterrupt:
    print("Stopping server.")
finally:
    conn.close()
    server.close()
    pipeline.stop()
    cv2.destroyAllWindows()