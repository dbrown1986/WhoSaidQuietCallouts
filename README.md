![Windows Build](https://github.com/dbrown1986/WhoSaidQuietCallouts/actions/workflows/windows-build.yml/badge.svg?branch=master)<br>

# ⚠️ Content Advisory – Suicide Attempt Callout  

This mod pack includes one optional callout titled **“Suicide Attempt”**, which portrays a scenario involving self‑harm and crisis intervention.  
It is **disabled by default** out of respect for all players.  
You may enable it at your own discretion by setting `SuicideAttempt=true` in the `[SuicideCallout]` section of the `WhoSaidQuietCallouts.ini` file.  

If you or someone you know struggles with thoughts of suicide or self‑harm, **please reach out for help**.  
- In the U.S., call **988** (Suicide and Crisis Lifeline).  
- Outside the U.S., visit [findahelpline.com](https://findahelpline.com) for international hotlines.

---

# :lady_beetle: Early Access Content - Bugs
This plugin is in early development at the moment, not ready for production.<br>
Please feel free to post in the issues section any bugs you are experiencing.
I will endeavor to work up a template for bug reports that would help when submitting.

---

# 🚔 Who Said Quiet Callouts (WSQ)

**Version:** 0.9.5 Stable · Reflective Integration & Documentation Build  
**Release Date:** March 9 2026  
**Engine:** RAGE Plugin Hook · LSPDFR 0.4.9 Compatible  

---

## 📜 Overview

Who Said Quiet Callouts (WSQ) is a modular, narrative‑driven callout pack for **LSPDFR** (Los Santos Police Department First Response) — designed around **realism**, **AI depth**, and **seamless plugin interoperability**.

Each dispatch is built as a storyline: AI actors respond, adapt, and evolve based on player interaction, time of day, and available support systems.

---

## 🧩 Features

### 🎮 Core Gameplay  
- **21 unique callouts**, each with branching AI and reactive behavior.  
- **Radiant AI Behavior Engine** adaptively switches Compliant / Fleeing / Hostile states.  
- **Cinematic dialogue support** via Grammar Police and custom audio keys.  
- **Single Active Callout System** to prevent conflicts with queued events.  
- **Dynamic Dispatch Cooldown** (randomized 30–300 seconds).  
- **Manual Launch Support** through Callout Interface (v1.9 +).  
- **Full reflective plugin integration** architecture (no hard dependencies).  

---

## 🔌 Integration Support (v0.9.5)

| Plugin | Functionality | Compatibility |
|:--|:--|:--|
| Stop The Ped (STP) | Advanced suspect interaction API | ⚠ Conflicts with Policing Redefined |
| Ultimate Backup (UB) | Backup / SWAT and responders | ⚠ Conflicts with Policing Redefined |
| Policing Redefined (PR) | AI pursuit and morale system | Replaces STP & UB |
| CompuLite | Records, citation and UI integration | ✅ Safe |
| Grammar Police | Dispatch audio / radio dialogue | ✅ Safe |
| Reports Plus | Custom callout reinforcement reports | ✅ Safe |
| LSPDFR Expanded | Supplemental agency codes / laws | ✅ Safe |
| External Police Computer | Extended MDT UI support | ✅ Safe |
| Callout Interface | Manual callout selection / replay | ✅ Safe |

---

## 🔥 Callout Library (v0.9.5 Stable)

*(All enabled by default except Suicide Attempt)*  

| # | Name | Description |
|:-:|:--|:--|
| 1 | Armed Robbery | Weapons drawn at local business; multi‑suspect setup. |
| 2 | Pursuit Suspect | Join an in‑progress vehicle pursuit. |
| 3 | Domestic Disturbance | Dynamic verbal dispute with escalating risk. |
| 4 | Suspicious Vehicle | Investigate a parked drug or theft vehicle. |
| 5 | Kidnapping | Locate and recover a kidnapped victim. |
| 6 | Gang Shootout | Large‑scale armed conflict AI event. |
| 7 | Burglary | Burglary in‑progress with compliance variance. |
| 8 | Animal Attack | Respond to animal‑on‑civilian attacks. |
| 9 | Public Intoxication | Handle minor disturbance or arrest. |
| 10 | Stolen Vehicle | Vehicle recovery / tracking. |
| 11 | Officer Down | Code 3 priority response. |
| 12 | Road Rage | Aggressive driver intervention. |
| 13 | Barricaded Suspects | Negotiation / SWAT scenario. |
| 14 | Speeding Vehicle | Traffic stop or pursuit decision. |
| 15 | Missing Person | Area search with dialogue prompts. |
| 16 | Drug Deal | Stakeout and bust mission. |
| 17 | VIP Escort | Convoy mission with AI protection. |
| 18 | Traffic Stop Assist | Officer backup on routine stop. |
| 19 | Welfare Check | Residential safety verification. |
| 20 | Stolen Police Vehicle | Locate and recover stolen marked unit. |
| 21 | 💬 Suicide Attempt | Optional sensitive callout (disabled by default). |

---

## ⚙️ Compiling From Source

### 🧰 Requirements
- Visual Studio 2019 or later  
- .NET Framework 4.8 SDK  
- RAGE Plugin Hook SDK (`LSPD_First_Response.dll`)  

### 🛠️ Steps
1. Clone or download this repository.  
2. Open `WhoSaidQuietCallouts.sln` in Visual Studio.  
3. Ensure reference DLLs are present:  
 • `RagePluginHook.dll` • `LSPD_First_Response.dll`<br>
 4. Build ( `Ctrl + Shift + B` ).  
5. Copy output files:  
 `WhoSaidQuietCallouts.dll` and `WhoSaidQuietCallouts.ini` → `Grand Theft Auto V/Plugins/LSPDFR`.  

> **Note:** Integrations are optional in‑game, included in code reflectively. 

---

## 💾 Installation

1. Download the latest release from GitHub or LSPDFR forums.  
2. Copy files to: `Grand Theft Auto V/Plugins/LSPDFR/`  
 - `WhoSaidQuietCallouts.dll`  
 - `WhoSaidQuietCallouts.ini`  
3. Edit the INI to toggle optional callouts and plugin support.  
4. Launch GTA V using RAGE Plugin Hook.  

---

## 🧠 Gameplay Tips

- Set dispatch cooldown range in the INI for frequency control.  
- Avoid running **Stop The Ped** and **Ultimate Backup** when **Policing Redefined** is enabled (similar features overlap).  
- If you use Policing Redefined, pair with **Reports Plus** or **External Police Computer** for record keeping; CompuLite is not supported by PR.  
- You can manually launch callouts through `F10` (Callout Interface).  
- Use `LogLevel = 3` for debug testing in logs.  

---

## 🧾 Changelog Summary

See [Docs/CHANGELOG.md](Docs/CHANGELOG.md) for the complete log.  
**Latest:** *Version 0.9.5 Stable — Reflective Integration & Maintenance Build (03/09/2026)*  

---

## 🧑‍💻 Development Team

**Who Said Quiet Team**  
- Lead Programming / Scenario Design · Project Lead  
- AI Assistance · Galaxy AI (GPT‑5 by OpenAI)  
- Testing / QA · Community Squad v2026  

---

## 💬 Special Thanks

- **LSPDFR Team** – plugin API foundation  
- **Albo1125 / BejoIjo** – Stop The Ped / Ultimate Backup frameworks  
- **RAGE Plugin Hook Developers**  
- **Integration Authors** (STP, UB, PR, CompuLite, Grammar Police, Reports Plus)  
- **Community Testers and Players** for support and feedback  

---

## 📬 Support & Feedback

- Submit issues on GitHub or LSPDFR forums (thread TBA).  
- Attach `Plugins/LSPDFR/Logs/WhoSaidQuietCallouts.txt` when reporting bugs.  

---

## 🏁 License

Licensed under [CC BY‑NC 4.0 Non‑Commercial Attribution](Docs/LICENSE.md).  
Redistribution allowed with credit; commercial use prohibited.  

---

## 🪟 Platform Support Notice (Windows Only)

WSQ is officially supported for **Windows** platforms only.  
Running under emulation (Wine, Proton, Crossover etc.) is **unsupported** and at the user’s own risk.  
No official technical support or issue tracking is offered for non‑Windows environments.  
