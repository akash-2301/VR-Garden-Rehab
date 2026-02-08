Project Title:
Garden Rehab – Upper Limb Rehabilitation Game

Overview:
Garden Rehab is a Unity-based rehabilitation game designed for stroke patients with upper-limb motor deficits. The game uses Intel RealSense hand-tracking and includes multiple therapy stages, scoring logic, and performance evaluation.

Key Features:

Hand tracking using Intel RealSense

12-stage rehab gameplay with increasing difficulty

Object picking mechanics (apple, mango, sunflower, rose)

Bee distraction logic

60-second timer per stage

Scoring, penalties, and performance factor

Three-attempt guidance system (no cue → outline → arrow)

Google Form data logging

How Calibration Works:

One-time hand calibration done using 4 reference points

Captured RealSense coordinates mapped to Unity screen space

Mapping reused across all levels without repeating calibration

Gameplay Summary:

Relevant objects enabled per stage

User picks the correct objects

Wrong object or exceeding limit gives penalty

Stage ends when timer finishes or all valid objects are collected

Performance Factor and PFF determine whether guidance cues appear

Performance Factor (PF):

If timerRemaining ≥ 1 → PF = 100%

Otherwise PF = score / maxScore (maxScore = total valid objects without bees)

PFF Calculation:
PFF = 0.2(PF1) + 0.3(PF2) + 0.5(PF3)

Folder Structure (Short):
/Assets/Scripts
/Assets/Prefabs
/Assets/Scenes
/Assets/HandTracking

How to Run the Project:

Clone or download this project

Open it in Unity

Connect the RealSense camera

Run Calibration Scene

Start Level 1

Requirements:

Unity 2021 or above

Intel RealSense SDK

RealSense camera (D435/D455)

Windows 10/11

Testing Notes:

Game can be tested on healthy individuals

Difficulty auto-adjusts based on performance

Cue assistance appears only in later attempts

Author:
Akash