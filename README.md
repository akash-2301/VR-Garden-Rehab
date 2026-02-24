# 🌿 Garden Rehab – Upper Limb Rehabilitation Game

A Unity-based neuro-rehabilitation system designed to improve upper-limb motor control using Intel RealSense hand tracking.

---

## 🖼️ Project UI Preview

![UI/UX Interface](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-23%20181322.png)

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

![Hand Calibration](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-23%20181439.png)

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

![Stages](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-23%20183947.png)

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

![Bee Stages](https://github.com/akash-2301/VR-Garden-Rehab/blob/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-23%20182934.png)

Bees sit on specific objects to block selection.

- Objects with bees **cannot be picked**  
- These are excluded from scoring  
- Adds cognitive + motor inhibition challenge  

---

## 🧠 Performance Factor Logic

![Performance Logic](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-24%20170152.png)

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

## Developer

**Akash Singh**
