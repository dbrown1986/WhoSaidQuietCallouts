# ⚠️ Content Advisory – Suicide Attempt Callout

This mod pack includes one optional callout titled **“Suicide Attempt”**, which portrays a scenario involving self‑harm and crisis intervention.  
It is **disabled by default** out of respect for all players.  
You may enable it at your own discretion by setting `SuicideAttempt=true` in the `[SuicideCallout]` section of the `WhoSaidQuietCallouts.ini` file.  

If you or someone you know struggles with thoughts of suicide or self‑harm, **please reach out for help**.  
- In the U.S., call **988** (Suicide and Crisis Lifeline).  
- Outside the U.S., visit [findahelpline.com](https://findahelpline.com), which provides international hotlines.

---

# 🚔 Who Said Quiet Callouts (WSQ)

**Version:** 1.9.1 · Maintenance & Documentation Cleanup Build  
**Release Date:** March 7 2026  
**Engine:** RAGE Plugin Hook · LSPDFR 0.4.9 Compatible  

---

## 📜 Overview
Who Said Quiet Callouts (WSQ) is a modular, narrative‑driven callout pack for **LSPDFR** (Los Santos Police Department First Response) built around realism, AI depth, and seamless plugin interoperability.  

Combining cinematic storytelling with professional police‑response logic, WSQ turns each dispatch into an evolving scenario shaped by choices, timing, and behavior AI states.

---

## 🧩 Features

### 🎮 Core Gameplay
- **21 original callouts**, each crafted with unique AI behavior and branching outcomes.  
- **Radiant AI Behavior Engine** dynamically selects between **Compliant**, **Fleeing**, and **Hostile** suspects.  
- **Narrative scripting** for immersive, story‑driven encounters.  
- **Single Active Callout System** — prevents overlapping scenes for stability.  
- **Dynamic Dispatch Cooldown** – random interval (30 – 300 seconds) between calls.  
- **Manual Callout Selection** via **Callout Interface Menu** (v1.9+) for full testing control.  
- **Wide plug‑in compatibility** with external LSPDFR tools (see table below).

| Integration | Function | Notes |
|:--|:--|:--|
| Stop The Ped (STP) | suspect control system | ⚠ conflicts with Policing Redefined |
| Ultimate Backup | tactical AI backup | ⚠ conflicts with Policing Redefined |
| Policing Redefined (PR) | AI & pursuit overhaul | replaces STP / UB |
| CompuLite | records & citation system | safe |
| Grammar Police | dispatch & radio audio | safe |
| Reports+ | enhanced incident summaries | safe |
| LSPDFR Expanded | agency / penal‑code extensions | safe |
| External Police Computer | advanced MDT UI | safe |
| Callout Interface | UI + manual callout selection | safe |

---

## 🔥 Callout Library (v1.9.1)
*(All callouts listed below are enabled by default except Suicide Attempt.)*

| # | Name | Description |
|:--:|:--|:--|
| 1 | Armed Robbery | Weapons‑drawn robbery scene. Multiple suspects. |
| 2 | Pursuit Suspect | Join an active pursuit and assist units. |
| 3 | Domestic Disturbance | Verbal / physical dispute with variable risk. |
| 4 | Suspicious Vehicle | Investigate a suspicious car parked unattended. |
| 5 | Kidnapping | Abduction response — time sensitive. |
| 6 | Gang Shootout | Area gunfight; multiple armed suspects. |
| 7 | Burglary | Break‑in in‑progress. Compliant / Flee / Hostile. |
| 8 | Animal Attack | Animal control or self‑defense incident. |
| 9 | Public Intoxication | Non‑lethal dialogue or arrest option. |
| 10 | Stolen Vehicle | Locate and recover reported theft. |
| 11 | Officer Down | Priority Code 3 backup situation. |
| 12 | Road Rage | Vehicle aggression or pursuit. |
| 13 | Barricaded Suspects | SWAT negotiation and entry scenario. |
| 14 | Speeding Vehicle | Traffic enforcement / reckless driver. |
| 15 | Missing Person | Search and rescue with dialogue closure. |
| 16 | Drug Deal | Narcotics surveillance takedown. |
| 17 | VIP Escort | Vehicle protection and route security. |
| 18 | Traffic Stop Assist | Code 2 backup on traffic stop. |
| 19 | Welfare Check | Residential welfare concern. |
| 20 | Stolen Police Vehicle | Code 3 response — stolen patrol unit. |
| 21 | 💬 **Suicide Attempt** | Optional sensitive scenario disabled by default — enable manually in INI. |

---

## ⚙️ Compiling From Source
### Requirements
- Visual Studio 2019 or later  
- .NET Framework 4.8 SDK  
- RAGE Plugin Hook SDK (LSPDFR dependencies)  

### Steps
1️⃣ Clone this repository or download ZIP.  
2️⃣ Open `WhoSaidQuietCallouts.sln` in Visual Studio.  
3️⃣ Add references to: `RagePluginHook.dll`, `LSPD_First_Response.dll`.  
4️⃣ Set build target → `.NET Framework 4.8` ( x64 ).  
5️⃣ Build Solution → compile to `bin/Release/WhoSaidQuietCallouts.dll`.  
6️⃣ Copy the DLL + INI to `GTA V/Plugins/LSPDFR/`.  

---

## 💾 Installation Instructions
1. Download or build WSQ.  
2. Place files into `Grand Theft Auto V/Plugins/LSPDFR/`.  
3. Enable desired integrations inside `WhoSaidQuietCallouts.ini`.  
4. Run **RAGE Plugin Hook**, verify console logs success.  
5. To enable the optional Suicide Attempt callout, edit:
   ```ini
   [SuicideCallout]
   SuicideAttempt=true
