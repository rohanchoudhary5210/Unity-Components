# Unity Firebase Leaderboard (Auto-Resize + Object Pooling)

## Overview

This leaderboard system displays the **top players sorted by score** using **Firebase Realtime Database**.
It updates automatically when scores change and uses **UI auto-resize and object pooling** for good performance on mobile devices.

Main features:

* Real-time leaderboard updates
* Top-N players sorted by score
* Auto-resizing UI using Unity layout components
* Object pooling (no repeated destroy/instantiate)
* Compatible with Firebase Realtime Database

---

# System Architecture

```
Firebase Database
      ↓
LeaderboardManager
      ↓
Object Pool (PlayerBox rows)
      ↓
ScrollView Content
      ↓
Vertical Layout Group + Content Size Fitter
```

---

# Database Structure

Leaderboard reads data from the `users` node.

```
users
   uid_1
      username: "alex"
      score: 1500

   uid_2
      username: "rohan"
      score: 2200

   uid_3
      username: "sam"
      score: 900
```

Scores determine ranking.

---

# Scripts

## LeaderboardManager.cs

Responsible for:

* Listening to Firebase leaderboard updates
* Sorting players
* Reusing UI rows via object pooling

Key behavior:

* Queries top players
* Updates UI when Firebase data changes
* Reuses existing UI rows instead of creating new ones

---

## PlayerBox.cs

Represents a **single leaderboard row**.

Handles displaying:

* Username
* Score
* Rank

Example row layout:

```
#1  Alex      5000
#2  Rohan     4200
#3  Sam       3100
```

---

# Unity UI Setup

## ScrollView Structure

```
Canvas
 └ LeaderboardPanel
    └ ScrollView
       └ Viewport
          └ Content
```

The **Content object is where rows are generated**.

---

## Content Object Components

Add these components to **Content**.

### Vertical Layout Group

Recommended settings:

```
Spacing: 120
Child Control Width: Enabled
Child Control Height: Enabled
Child Force Expand Width: Enabled
Child Force Expand Height: Disabled
Padding Top & Bottom Required (Check Image)
<img width="304" height="332" alt="image" src="https://github.com/user-attachments/assets/6a65ee68-c560-4492-ba17-b0beb7d18e02" />

```

Purpose:

* Automatically stacks leaderboard rows vertically.

---

### Content Size Fitter

```
Horizontal Fit: Unconstrained
Vertical Fit: Preferred Size
```

Purpose:

* Automatically increases the content height when rows are added.

---

# Row Prefab Setup

Create a prefab named `LeaderboardRow`.

Hierarchy example:

```
LeaderboardRow
   BackgroundImage
   RankText
   UsernameText
   ScoreText
```

Attach **PlayerBox.cs** to the prefab.

Recommended components:

* Image (background)
* TextMeshProUGUI for text
* Layout Element

Example Layout Element settings:

```
Preferred Height: 60
Flexible Height: 0
```

---

# Leaderboard Update Flow

```
Player beats score
        ↓
Score saved to Firebase
        ↓
Firebase ValueChanged event triggers
        ↓
LeaderboardManager receives update
        ↓
UI rows updated from pool
        ↓
Content container resizes automatically
```

---

# Performance Optimization

### Object Pooling

Rows are reused instead of destroyed.

Benefits:

* No garbage collection spikes
* Better mobile performance
* Smooth UI updates

---

### Auto-Resize Layout

Unity layout components handle resizing automatically.

Benefits:

* No manual UI resizing code
* Compatible with ScrollView
* Works with dynamic player counts

---

# Typical Usage

Update a player's score:

```
UpdateHighScore(score);
```

Leaderboard updates automatically due to Firebase listener.

---

# Troubleshooting

### Rows overlap

Ensure **Content has Vertical Layout Group**.

### Image not visible

Check:

* Image sprite assigned
* Alpha > 0
* Scale = (1,1,1)

### Leaderboard not updating

Verify:

* Firebase connection active
* Database path is `users`
* Score field exists.

---

# Future Improvements

Possible extensions:

* Global ranking system
* Player rank outside top leaderboard
* Friend leaderboards
* Rank change animations
* Top-100 pagination

---

# Summary

This leaderboard system provides a **simple real-time ranking display** backed by Firebase.
Using **layout groups and pooling**, it remains efficient and scalable for mobile games.
