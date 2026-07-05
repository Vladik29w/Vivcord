# Vivcord

> ⚠️ **Note:** This project is actively **under development** (Work in Progress). Features, architecture, and deployment configurations are subject to change.

Vivcord is a real-time messaging and voice communication platform built with modern web technologies. It allows users to manage friends, participate in real-time text chats, and join voice channels seamlessly.

## 🚀 Tech Stack

* **Frontend:** Angular
* **Backend:** ASP.NET Core (C#)
* **Real-time Messaging:** SignalR
* **Voice/Video (SFU):** LiveKit
* **Database:** Entity Framework Core

## ☁️ Infrastructure & Hosting

* **Web Hosting:** The main application (Frontend client and Backend APIs) is hosted on **Microsoft Azure**.
* **SFU (Voice/Video):** Real-time media handling is powered by [LiveKit](https://livekit.io/), self-hosted on an **Oracle Virtual Machine**.

## ✨ Current Features (WIP)

* **User Authentication:** Secure registration, login, and JWT-based session management.
* **Friend System:** Send, accept, or decline friend requests and manage your friend list.
* **Direct Messaging:** Real-time private text chat using SignalR hubs.
* **Voice Chat:** Low-latency, reliable voice communication channels using LiveKit.
* **User Profiles:** Basic user profile management.

*Link to main page and instructions for local setup and docker deployment will be expanded as the project stabilizes.*
