# Vivcord

[![Deploy Status](https://img.shields.io/badge/Deployment-Azure-blue?style=flat-square&logo=microsoft-azure)](#)
[![Backend](https://img.shields.io/badge/.NET-ASP.NET_Core-512BD4?style=flat-square&logo=dotnet)](#)
[![Frontend](https://img.shields.io/badge/Angular-DD0031?style=flat-square&logo=angular)](#)

> **🌐 [Open Vivcord (Live Website)](https://vivcord-frontend.azurewebsites.net/)**

**Vivcord** is a modern web messenger designed for real-time communication. The project is built with a focus on speed, seamless user experience, and advanced communication features, including text messaging and voice chats.

## Key Features

* **Real-Time Messaging:** Instant message delivery in private and group chats powered by WebSockets (SignalR).
* **Voice Chats:** Seamless audio streaming integration using LiveKit for high-quality voice communication.
* **GIF Integration:** Quick search and sending of GIF animations via the Klipy API.
* **Security:** User authentication and authorization system using JWT tokens with ablity to login via Google account with Google OAuth sytem.
* **Private & Group Rooms:** Flexible chat creation system for direct personal messaging and group communication.

## Tech Stack

**Backend:**
* C# / ASP.NET Core
* Entity Framework Core
* SQL Server (MSSQL)
* SignalR (Real-time communication)
* ErrorOr (result pattern error handling)
* LiveKit Server SDK (Voice streaming)

**Frontend:**
* Angular 21
* RxJS
* Angular Signals

**Infrastructure & DevOps:**
* Docker
* Azure container app (backend hosting)
* Azure app service (frontend hosting)
* GitHub Actions (CI/CD pipelines)
* Azure blob storage (user's ppf and files storage)
* Azure communication service (email sending)
* Oracle VM (Livekit hosting)

**External APIs**
* Google OAuth API
* Klipy API
