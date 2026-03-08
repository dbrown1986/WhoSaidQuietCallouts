# ⚠️ Content Advisory – Suicide Attempt Callout  

This mod pack includes one optional callout titled **“Suicide Attempt”**, which portrays a scenario involving self‑harm and crisis intervention.  
It is **disabled by default** out of respect for all players.  
You may enable it at your own discretion by setting `SuicideAttempt=true` in the `[SuicideCallout]` section of the `WhoSaidQuietCallouts.ini` file.  

If you or someone you know struggles with thoughts of suicide or self‑harm, **please reach out for help**.  
- In the U.S., call **988** (Suicide and Crisis Lifeline).  
- Outside the U.S., visit [findahelpline.com](https://findahelpline.com) for international hotlines.

---

![Build Failing](https://img.shields.io/badge/Windows%20Build-failing-red?logo=windows&style=for-the-badge)

---

# 🚔 Who Said Quiet Callouts (WSQ)  

**Version:** 1.9.1 · Maintenance & Documentation Cleanup Build  
**Release Date:** March 7 2026  
**Engine:** RAGE Plugin Hook · LSPDFR 0.4.9 Compatible  

---

## 📜 Overview  
Who Said Quiet Callouts (WSQ) is a modular, narrative‑driven callout pack for **LSPDFR** (Los Santos Police Department First Response) built around realism, AI depth, and seamless plugin interoperability.  

By blending cinematic scenarios with advanced behavior logic, WSQ turns each dispatch into a unique story — shaped by your choices, response time, and AI conditions.

---

## 🧩 Features

### 🎮 Core Gameplay  
- **21 original callouts**, each crafted with dynamic AI and scenario branching.  
- **Radiant AI Behavior Engine** (enforces Compliant / Fleeing / Hostile transitions).  
- **Narrative dialogues** with realistic speech and situational responses.  
- **Single Active Callout System** — prevents overlapping events.  
- **Dynamic Dispatch Cooldown** (randomized 30–300 seconds).  
- **Manual Callout Selection** through the Callout Interface (v1.9+).  
- **Integrations with popular LSPDFR plugins** (see below).  

---

### 🔌 Integration Support  
| Plugin | Function | Compatibility |
|:--|:--|:--|
| Stop The Ped (STP) | Advanced suspect control | ⚠ Conflicts with Policing Redefined |
| Ultimate Backup (UB) | Backup & SWAT AI | ⚠ Conflicts with Policing Redefined |
| Policing Redefined (PR) | AI pursuit overhaul | Replaces STP & UB |
| CompuLite | Citation system & records integration | ✅ Safe |
| Grammar Police | Dispatch / radio audio | ✅ Safe |
| Reports+ | Enhanced incident reports | ✅ Safe |
| LSPDFR Expanded | Additional agency & penal codes | ✅ Safe |
| External Police Computer | Extended MDT UI System | ✅ Safe |
| Callout Interface | Manual menu callout control | ✅ Safe |

---

## 🔥 Callout Library (v1.9.1)
*(All callouts enabled by default except Suicide Attempt.)*  

| # | Name | Description |
|:--:|:--|:--|
| 1 | Armed Robbery | Weapons drawn at local business — multi‑suspect setup. |
| 2 | Pursuit Suspect | Join an active vehicle pursuit and assist. |
| 3 | Domestic Disturbance | Verbal dispute with dynamic threat levels. |
| 4 | Suspicious Vehicle | Investigate parked vehicle with drug activity. |
| 5 | Kidnapping | Locate and rescue victim (urgent). |
| 6 | Gang Shootout | Area‑wide armed conflict between NPC groups. |
| 7 | Burglary | B&E in‑progress with compliance or flee variance. |
| 8 | Animal Attack | Animal control assistance or protection response. |
| 9 | Public Intoxication | Non‑lethal public disturbance handling. |
| 10 | Stolen Vehicle | Locate and recover reported vehicle theft. |
| 11 | Officer Down | Code 3 priority backup call. |
| 12 | Road Rage | AI traffic aggression scenario. |
| 13 | Barricaded Suspects | Tactical SWAT standoff event. |
| 14 | Speeding Vehicle | Traffic enforcement scenario. |
| 15 | Missing Person | Search callout with dialogue‑based outcomes. |
| 16 | Drug Deal | Undercover observation and arrest scene. |
| 17 | VIP Escort | Convoy protection mission. |
| 18 | Traffic Stop Assist | Officer backup on traffic stop. |
| 19 | Welfare Check | Residential safety check. |
| 20 | Stolen Police Vehicle | Code 3 response to stolen marked unit. |
| 21 | 💬 Suicide Attempt | Optional sensitive callout (disabled by default). |

---

## ⚙️ Compiling WSQ from Source  

### 🧰 Requirements  
- Microsoft Visual Studio 2019 or later  
- .NET Framework 4.8 SDK  
- RAGE Plugin Hook SDK (`LSPD_First_Response.dll` required)  

### 🛠️ Steps  
1. Clone or extract this repository.  
2. Open `WhoSaidQuietCallouts.sln` in Visual Studio.  
3. Copy reference DLL's to source directory:  
   - `RagePluginHook.dll` - `LSPD_First_Response.dll` - `UltimateBackup.dll` - `StopThePed.dll` - `ReportsPlus.dll` - `CompuLite.dll` - `GrammarPolice.dll` - `PolicingRedefined.dll` - `LSPDFR_Expanded.dll` - `CalloutInterface.dll`
5. Build solution (`Ctrl+Shift+B`).  
6. Copy `WhoSaidQuietCallouts.dll` and `WhoSaidQuietCallouts.ini` to `GTA V/Plugins/LSPDFR`.  

**PLEASE NOTE:** Integrations are optional within the game, but must be present at time of compile.
If you have downloaded the latest release package, these DLL's are not required unless you choose
to run the mods in your game session.

---

## 💾 Installation  
1. **Download** the compiled WSQ release.  
2. Copy the following files into `Grand Theft Auto V/Plugins/LSPDFR/`:  
   - `WhoSaidQuietCallouts.dll`  
   - `WhoSaidQuietCallouts.ini`  
3. Enable optional integrations in the INI file.  
4. Start GTA V via RAGE Plugin Hook.  

---

## 🧠 Gameplay Tips  
- Adjust callout frequency via cooldown range in INI.  
- Avoid running StopThePed & Ultimate Backup with Policing Redefined simultaneously.
- If you're running Policing Redefined, you'll likely want to use Reports+ or ExternalPoliceComputer, as Compulite is not supported.
- You can launch specific calls through F10 (Callout Interface).  
- Use `LogLevel=3` for debug testing.  

---

## 🧾 Changelog Summary  
Full history available in [CHANGELOG.md](Docs/CHANGELOG.md).  
**Latest:** v1.9.1 — Maintenance & Documentation Cleanup (Build 03/07/2026).  

---

## 🧑‍💻 Development Team  
**Who Said Quiet Team**  
- Lead Programming / Design · Project Lead  
- AI Assistance · Galaxy AI (GPT‑5 Model by OpenAI)  
- Testing / QA · Community Squad v2026  

---

## 💬 Special Thanks  
- **LSPDFR Team** – plugin API foundation.  
- **Albo1125 / BejoIjo** – STP / UB inspiration & frameworks.  
- **RAGE Plugin Hook Developers**.  
- **Integration Authors** (STP, UB, PR, CompuLite, Grammar Police etc.).  
- **Players and Testers** for ongoing support.  

---

## 📬 Support & Feedback  
Report issues on GitHub or via LSPDFR forums (thread TBA).  
Include `Plugins/LSPDFR/Logs/WhoSaidQuietCallouts.txt` in tickets.  

---

## 🏁 License  
Licensed under [CC BY‑NC 4.0 Non‑Commercial Attribution License](Docs/LICENSE.md).  
Redistribution with credit only · No commercial use.  

---

## 🪟 Platform Warning (Windows Only)
This project is **released and supported for Windows only**.  
If you are running WSQ under emulation or compatibility layers such as **Wine, Proton, or Crossover on Linux or macOS** this is **unsupported**.
Use of third‑party wrappers may cause instability or data loss and is done **solely at the user’s own risk**.  
No technical support or issue tracking is offered for non‑Windows environments.  
