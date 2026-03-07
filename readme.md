# 🚔 Who Said Quiet Callouts (WSQ)

**Version:** 1.9.1 · Maintenance & Documentation Cleanup Build  
**Release Date:** March 7 2026  
**Engine:** RAGE Plugin Hook · LSPDFR 0.4.9 Compatible  

---

## 📜 Overview
Who Said Quiet Callouts (WSQ) is a modular, narrative‑driven callout pack for **LSPDFR** (Los Santos Police Department First Response) built around realism, AI depth, and seamless plugin interoperability.  

Combining cinematic storytelling with professional police‑response logic, WSQ turns each dispatch into an evolving scenario shaped by choices, timing, and behavior AI states.

---

## 🧩 Features

### 🎮 Core Gameplay
- **21 original callouts**, each crafted with unique AI behavior and branching outcomes.  
- **Radiant AI Behavior Engine** 
  - suspects shift between **Compliant**, **Fleeing**, and **Hostile** modes dynamically.  
- **Narrative scripting** for immersive scenes and authentic dialogue.  
- **Single Active Callout System** — prevents overlapping scenes for performance consistency.  
- **Dynamic Dispatch Cooldown** – realistic random interval (30 – 300 seconds) between calls.  
- **Manual Callout Selection** via **Callout Interface Menu** (v1.9+)  
  - start, cancel, or review WSQ calls through menu interface.  
- **Full integration support** for external LSPDFR plugins:

| Integration | Function | Notes |
|:--|:--|:--|
| Stop The Ped (STP) | advanced suspect control | conflicts with Policing Redefined |
| Ultimate Backup | backup and SWAT AI units | conflicts with Policing Redefined |
| Policing Redefined (PR) | AI & pursuit overhaul | replaces STP and UB features |
| CompuLite | records / citation integration | safe |
| Grammar Police | dispatch and radio audio | safe |
| Reports+ | enhanced incident reporting | safe |
| LSPDFR Expanded | new agencies and code definitions | safe |
| External Police Computer | extended MDT UI system | safe |
| Callout Interface | in‑game UI + manual callout selection | safe |

---

## 🔥 Callout Library _(v1.9.1)_
| # | Name | Description |
|:--:|:--|:--|
| 1 | Armed Robbery | Weapons drawn at store location — multi‑suspect standoff |
| 2 | Pursuit Suspect | Ongoing vehicle pursuit — join and assist units |
| 3 | Domestic Disturbance | Dispute with variable threat levels |
| 4 | Suspicious Vehicle | Investigate parked vehicle, possible drug activity |
| 5 | Kidnapping | Locate abducted subject — time‑critical response |
| 6 | Gang Shootout | Multiple armed suspects engaged in open fire |
| 7 | Burglary | B&E in‑progress with random flee/compliance states |
| 8 | Animal Attack | Animal control / suspect protective actions |
| 9 | Public Intoxication | Disorderly person — non‑lethal dialogue option |
| 10 | Stolen Vehicle | Reported theft — track and recover |
| 11 | Officer Down | Backup priority Code 3 — scene defense |
| 12 | Road Rage | AI driver pursuit behavior simulation |
| 13 | Barricaded Suspects | Tactical SWAT support situation |
| 14 | Speeding Vehicle | Traffic enforcement / reckless operation |
| 15 | Missing Person | Search pattern scenario with dialogue endings |
| 16 | Drug Deal | Narcotics surveillance and takedown |
| 17 | VIP Escort | Protective convoy mission |
| 18 | Traffic Stop Assist | Code 2 backup for officer traffic stop |
| 19 | Welfare Check | Knock‑and‑talk for well‑being confirmation |
| 20 | Stolen Police Vehicle | Code 3 response for stolen marked unit |
| 21 | **Suicide Attempt (Callout Special)** | Persuasion / medical response / helpline overlay |

---

## ⚙️ Compiling WSQ from Source

### 🧰 Requirements
- **Microsoft Visual Studio 2019** or later  
- **.NET Framework 4.8 SDK**  
- **RAGE Plugin Hook SDK** (`LSPD_First_Response.dll` included in your LSPDFR install)  

### 🛠️ Steps
1. Clone or extract this repository into your projects directory.  
2. Open `WhoSaidQuietCallouts.sln` in Visual Studio.  
3. In **Project > Add Reference**, link:
   - `RagePluginHook.dll`  
   - `LSPD_First_Response.dll`  
4. Ensure build target is: `.NET Framework 4.8`, x64.  
5. Press **Build > Build Solution** ( `Ctrl + Shift + B` ).  
6. Output:  
   `bin/Release/WhoSaidQuietCallouts.dll`  

Place that DLL and the INI/Integration folders into your:  
`Grand Theft Auto V/Plugins/LSPDFR/` directory.

---

## 💾 Installation Instructions

1. **Download** the compiled WSQ package or build it yourself.  
2. **Copy**:
   - `WhoSaidQuietCallouts.dll`  
   - `WhoSaidQuietCallouts.ini`  
   into `GTA V/Plugins/LSPDFR/`.  
3. Optional Integrations: install corresponding mods (STP, UB, CompuLite, etc.).  
4. Open `WhoSaidQuietCallouts.ini` and toggle desired callouts/plugins:
   ```ini
   StopThePed=true
   UltimateBackup=false
   GrammarPolice=true
