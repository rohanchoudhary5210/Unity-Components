# Scene Reference Finder for Unity

A powerful, lightweight Unity Editor tool designed to track down object references and component usage within your scenes.

Whether you're refactoring code, cleaning up missing references, or swapping out assets, this tool provides a visual and interactive way to manage your scene dependencies.

---

## ✨ Features

### 🔍 Reference Searching

Find every `SerializedProperty` (Inspector field) that points to a specific object such as:

* Materials
* Textures
* Prefabs
* Any Unity asset

---

### 🧩 Component Locating

Quickly locate all `GameObjects` that have a specific script or component attached.

---

### 🌿 Visual Hierarchy Highlighting

* Matching objects are highlighted **green directly in the Hierarchy window**
* Makes it easy to visually scan results instantly

---

### 🎯 Interactive Results

* Use the **Ping** button to:

  * Focus on the object
  * Highlight it in the Hierarchy
  * Select it in the Inspector

---

### 🔁 Batch Replacement

* Replace all found references with a new object in **one click**
* Fully integrated with Unity's **Undo system** (`Ctrl + Z` supported)

---

### 🧠 Deep Scan

* Searches through:

  * Arrays
  * Lists
  * Nested classes and structs

---

## 📖 How to Use

### 1. Installation

Place the script inside an `Editor` folder:

```
Assets/Editor/SceneReferenceFinder.cs
```

> This ensures the tool is only included in the Unity Editor and not in your final build.

---

### 2. Opening the Tool

Navigate to:

```
Tools > Scene Reference Finder
```

A dockable window will appear.

---

### 3. Selecting a Search Mode

Choose from the **Search Mode** dropdown:

* **Find References**

  * Use this to locate where an asset is used

* **Find Components**

  * Use this to locate where a script/component is attached

---

### 4. Running a Search

1. Drag your target object or script into the **Target** field
2. Click **Search**

Results will show:

* GameObject name
* Property path where the reference exists

✔ Matching objects will also be highlighted in green in the Hierarchy.

---

### 5. Managing Results

* **Ping**

  * Highlights and selects the object in the scene

* **Clear Highlights**

  * Removes all green highlights from the Hierarchy

---

### 6. Batch Replacement (Optional)
<img width="320" height="607" alt="image" src="https://github.com/user-attachments/assets/dc6cd631-21f9-40c5-8e36-01624c2c0d88" />

To replace all found references:

1. Enable **Replace**
2. Assign a **Replacement Object**
3. Click **Replace All**

> You can undo the operation using `Ctrl + Z`

---

## 🛠 Technical Requirements

* **Unity Version:** 2019.4 LTS or newer
* **Scope:**

  * Searches the **currently active scene**
  * Includes **inactive GameObjects**

---

## 💡 Pro Tips

### 🔧 Find Missing Scripts

Use the tool to track down broken references like:

```
Missing (MonoBehaviour)
```

---

### 🧹 Clean Up Layers or Systems

Before removing or refactoring a script:

* Use **Find Components**
* Identify all dependent objects
* Safely update or remove them

---

## 🚀 Summary

Scene Reference Finder helps you:

* Debug faster
* Refactor safely
* Manage dependencies visually

A must-have utility for keeping your Unity scenes clean and maintainable.

---
