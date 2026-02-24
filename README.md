# 🌿 Garden Rehab – Upper Limb Rehabilitation Game

A Unity-based neuro-rehabilitation system designed to improve upper-limb motor control using Intel RealSense hand tracking.

---

## 🖼️ Project UI Preview
👉 (Attach Image 1 – UI/UX Interface)

![UI/UX Interface](path/to/image1.png)

---

## 📌 Overview

**Garden Rehab** is a serious-game rehabilitation system built in Unity for stroke patients with upper-limb motor impairment.  
It integrates RealSense-based hand tracking, multi-stage therapy tasks, adaptive cues, and performance-based scoring to support motor recovery and engagement.

---

## ✨ Key Features

- 🎮 **12 rehab stages** with progressive difficulty  
- ✋ **Intel RealSense hand tracking** (D435/D455)  
- 🍎 **Object-picking gameplay** (apple, mango, sunflower, rose)  
- 🐝 **Bee distraction mechanic**  
- ⏱️ **60-second timer** per stage  
- 🧮 **Scoring, penalties & performance factor**  
- 🎯 **Three-attempt adaptive cueing:**  
  - Attempt 1 → No cue  
  - Attempt 2 → Outline  
  - Attempt 3 → Arrow + bloom  
- 📊 **Google Form data logging** for performance analysis  

---

## ✋ How Calibration Works
👉 (Attach Image 2 – Hand Calibration)

![Hand Calibration](path/to/image2.png)

Calibration is done once using four hand positions:

1. Right  
2. Up-Right (Diagonal)  
3. Up  
4. Down  

These RealSense points are mapped to Unity’s screen space to determine personalized movement boundaries.

### ✔️ Calibration Advantages  
- Saved one time  
- Reused across all levels  
- No repeated calibration required  

---

## 🎮 Gameplay Summary
👉 (Attach Image 3 – Stages Showcase)

![Stages](path/to/image3.png)

Each stage activates certain objects. The user must pick only the correct ones.

### ✔️ Correct Mechanics

- Correct object → **+Score**  
- Wrong object → **Penalty**  
- Exceeding allowed limit → **Penalty + red-circle alert**  

### ✔️ Stage Ends When:
- Timer finishes **OR**  
- All valid (bee-free) objects are collected  

---

## 🐝 Stages With Bee Constraints
👉 (Attach Image 4 – Bee Constraint Stages)

![Bee Stages](path/to/image4.png)

Bees sit on specific objects to block selection.

- Objects with bees **cannot be picked**  
- These are excluded from scoring  
- Adds cognitive + motor inhibition challenge  

---

## 🧠 Performance Factor Logic

### **1️⃣ Performance Factor (PF)**  

- If **timerRemaining ≥ 1 sec** → PF = **100%**  
- Else:  
  ```
  PF = score / maxScore
  ```
where  
**maxScore = total valid objects without bees**

---

### **2️⃣ Weighted PFF Calculation**
```
PFF = 0.2(PF1) + 0.3(PF2) + 0.5(PF3)
```

---

### **3️⃣ Cue Trigger Logic**

If **PFF < 70**, next cue level activates:

- No cue → Outline → Arrow  

---

## 📁 Folder Structure (Brief)

```
/Assets
   /Scripts
   /Scenes
   /Prefabs
   /HandTracking
```

---

## 🚀 How to Run the Project

1. Clone/download the repository  
2. Open in **Unity 2021+**  
3. Connect Intel RealSense camera  
4. Run the **Calibration Scene**  
5. Start from **Level 1**  

---

## 🧩 Requirements

- Unity **2021 or newer**  
- Intel RealSense **SDK**  
- RealSense **D435 / D455** camera  
- Windows **10/11**  

---

## 🧪 Testing Notes

- Can be tested with healthy participants  
- Difficulty adapts automatically  
- Cue assistance appears only in later attempts  
- Intended for clinical + home-based rehabilitation  

---

## ✍️ Author

**Akash Singh**
