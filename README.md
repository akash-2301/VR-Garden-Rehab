# 🌿 Garden Rehab – Upper Limb Rehabilitation Game

A Unity-based neuro-rehabilitation system designed to improve upper-limb motor control using Intel RealSense hand tracking.

---

## 🖼️ UI Preview

![UI/UX Interface](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-23%20181322.png)

---

## 📌 Overview

Garden Rehab is a clinically-inspired serious game built for stroke and neuro-rehabilitation patients with impaired upper-limb function.
The system blends gamified object-picking tasks with precise hand tracking, creating a motivating and measurable therapy environment.

The game consists of:

- Multiple therapy stages  
- Hand-calibration–based interaction  
- Adaptive guidance (no cue → outline → arrow)  
- Performance scoring & data logging  

This enables therapists to track improvements and adjust training difficulty in a structured way.

---

## ✨ Key Features

- 🎮 12 progressively challenging rehab stages  
- ✋ Real-time hand tracking using Intel RealSense (D435/D455)  
- 🍎 Object-picking therapy tasks (apple, mango, sunflower, rose)  
- 🐝 Bee distraction mechanic to test attention & impulse control  
- ⏱️ 60-second timer per stage  
- 🧮 Scoring, penalties, and performance factor computation  
- 🎯 Three-attempt adaptive cueing system:  
  - Attempt 1: No cue  
  - Attempt 2: Outline cue  
  - Attempt 3: Arrow + bloom highlight  
- 📊 Automatic Google Form data logging for performance analysis  

---

## ✋ How Calibration Works

![Hand Calibration](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-23%20181439.png)

Calibration is performed once and reused across all scenes.  
The user moves their hand into four predefined directions:

- Right  
- Up-Right (Diagonal)  
- Up  
- Down  

These RealSense 2D coordinates are then mapped to Unity screen space, creating a personalized boundary that accurately reflects each patient’s reachable range.

### ✔️ Advantages

- Mapping saved after calibration  
- Consistent hand tracking across all stages  
- No repeated calibration needed during a session  

---

## 🎮 Gameplay Summary

![Stages](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-23%20183947.png)

Each stage activates specific objects, and the player must pluck only the correct ones:

### ✔️ Correct Mechanics

- Correct object → +Score  
- Wrong object → Penalty  
- Exceeding stage limit → Penalty + visual red-circle alert  

### 🕹️ Stage Completion Conditions

A stage ends when:

- The 60-second timer finishes, OR  
- All valid objects (those without bees) are collected  

---

## 🐝 Stages With Bee Constraints

![Bee Stages](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-23%20182934.png)

Bees act as interactive distractors:

- Objects with bees cannot be picked  
- These objects are not counted in scoring or maxScore  
- Improves selective attention and motor inhibition  

---

## 🧠 Performance Factor Logic

![Performance Logic](https://github.com/akash-2301/VR-Garden-Rehab/raw/7c04b431efea9bcaeb6885806f42ae2b9b474b02/Images/Screenshot%202026-02-24%20170152.png)

### 1️⃣ Performance Factor (PF)

If **timerRemaining ≥ 1 sec** → PF = **100%**  
Otherwise:

```
PF = score / maxScore
```

Where:  
**maxScore = number of valid (bee-free) objects**

---

### 2️⃣ Weighted PFF Calculation

```
PFF = 0.2(PF1) + 0.3(PF2) + 0.5(PF3)
```

---

### 3️⃣ Cue Trigger Logic

If **PFF < 70**, the system advances to the next cue level:

- No cue → Outline → Arrow  

This ensures adaptive difficulty based on user performance.

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

## 🧩 Requirements

- Unity 2021+  
- Intel RealSense SDK  
- RealSense D435 / D455  
- Windows 10 or 11  

---

## 🧪 Testing Notes

- Can be safely tested on healthy individuals  
- Difficulty automatically adjusts based on performance  
- Cue assistance appears only in later attempts  
- Designed for clinical training and motor recovery assessment  

---

## Developer

**Akash Singh**
