# Saclisam ⚔️🌍

![Unity](https://img.shields.io/badge/Engine-Unity_2022%2B-black) ![C#](https://img.shields.io/badge/Language-C%23-blue) ![Architecture](https://img.shields.io/badge/Architecture-Data_Driven-green) ![Status](https://img.shields.io/badge/Status-Prototype-orange)

> **Concept:** An Asynchronous Open-World Turn-Based Roguelike RPG, where the world evolves independently of player intervention.

---

## ⚠️ Scope Note
*This document serves as a **Technical Summary and Architecture Showcase**. It does not represent the final Game Design Document (GDD). It outlines the fundamental pillars and technical solutions implemented to date.*

---

## 🌌 Vision & Core Mechanics
Saclisam breaks away from traditional linear storytelling. The world is a **living ecosystem** that progresses asynchronously. The game does not hold the player's hand; instead, it encourages adaptation through trial and error in a hostile environment.

### 💀 Roguelike Mechanics & Dynamic Death
Death in Saclisam is not a simple "Game Over"; it is an event that alters the game state:

1.  **Classic Reset:** By default, death is permanent. If the player dies without safeguards (such as lineage or cloning), the run ends, and **the world resets**, generating a new history.
2.  **Persistence & "Echoes":** Under special conditions (e.g., dying as a high-level or corrupted character), **the world does NOT reset**.
    * The player starts with a fresh character in the same persistent world instance.
    * The previous character does not vanish: they transform into an **"Undead" or "Fallen" entity**.
    * The new character can encounter, battle, and loot their former incarnation, which now acts as a formidable enemy retaining the stats and equipment held at the moment of death.

---

## ⚙️ Technical Architecture
Saclisam's core relies on a **Data-Driven Architecture** to manage the complexity of a persistent open world.

### 1. Entity System & ScriptableObjects (SOs)
Logic is abstracted into `ScriptableObjects` to prevent hard coupling.
* **Global SOs:** Skills, Items, and Enemy Definitions are independent assets.
* **Entity Controller:** An agnostic controller that injects logic at runtime.
* **Inheritance & Polymorphism:** Utilizes generic interfaces (like `ICombatInterface`) to manage universal interactions between NPCs, the Player, and the Environment.

### 2. State Management & Flags
Extensive use of **Bitwise Flags** (`System.Flags`) to manage complex attributes (Damage Types, Status Effects, Equipment Permissions) efficiently and scalably.

### 3. Combat & Turn Manager
Tactical combat instantiated within the real world.
* **Dynamic Grid System:** Generates a tactical grid over the terrain upon combat initiation.
* **Multi-Instance:** Supports multiple simultaneous combat encounters in different map chunks.
* **Modular AI:** Each enemy entity manages its own decision-making logic and states (Patrol, Chase, Combat) both inside and outside of battle.

### 4. Data Persistence (Save System)
A robust local save system designed for a Single Player environment.
* **Format:** **JSON Serialization** of complex objects (Stats, Inventory, World State).
* **Security:** Save file encryption (Obfuscation) to prevent accidental corruption and discourage trivial external editing.
* **Path:** Secure storage in `PersistentDataPath` for cross-platform compatibility.

---

## 🚀 Optimization & Performance
* **Chunk System:** Asynchronous terrain loading/unloading.
* **Rendering:** Occlusion Culling and Dynamic Fog to optimize the Low Poly rendering pipeline.
* **Object Pooling:** Reutilization of entities and particles to minimize Garbage Collection spikes.

---

## 🛠️ Stack & Tools
* **Engine:** Unity (URP - Universal Render Pipeline).
* **Data:** ScriptableObject Architecture + Encrypted JSON.
* **Version Control:** Git.

---

## 🚧 Roadmap / In Development

* [ ] **Evolution System:** Refactoring of class talent trees and evolutionary paths.
* [ ] **Crafting System:** Item creation logic and alchemy.
* [ ] **Behavior Trees:** Implementation of advanced AI for complex enemies.
* [ ] **Asynchronous Persistence:** Background saving to ensure seamless gameplay.

---

### Author / Contact
**Nazareno Negrete**

📧 Email: [nazareno.negrete22@gmail.com](mailto:nazareno.negrete22@gmail.com)
