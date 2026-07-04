# Secure Backend-for-Frontend (BFF) Authentication with Angular 22, .NET 10, & Keycloak

This repository demonstrates a production-grade implementation of the **Backend-for-Frontend (BFF) Authentication Pattern** migrating from an insecure cross-domain configuration to a unified same-domain architecture. By leveraging **YARP (Yet Another Reverse Proxy)**, this setup routes all traffic under a single domain, completely eliminating the need for CORS middleware and unlocking the highest level of browser cookie security (`SameSite=Strict` / `Lax`).

---

## 📖 Detailed Walkthrough & Guide
> 💡 **Looking for the full architectural breakdown?** A comprehensive, step-by-step discussion explaining the security theory, code implementation, and deep-dive configurations can be found in my accompanying **[Medium Article Series]**. Check it out for a complete guide on moving from development prototypes to production-grade security!

---

## 🚀 Architecture Overview

Instead of the Angular 22 SPA interacting directly with Keycloak or a cross-domain API service, all incoming traffic flows through a unified domain context.

* **Frontend SPA:** Angular 22 (Running on `app.company.local`)
* **Reverse Proxy / BFF Gateway:** .NET 10 Web API powered by **YARP** (Running on `api.company.local`)
* **Identity Provider (IdP):** Keycloak (Handling OIDC/OAuth2 protocol steps downstream from the BFF)

---

## 🛠️ Prerequisites

Before launching the services, ensure your development environment has:
* **.NET 10 SDK** or higher
* **Node.js & npm** (Compatible with Angular 22)
* **Keycloak Server** (Running locally, standalone, or via Docker container)

---

## ⚙️ Identity Provider Setup (Keycloak)

1. **Create/Select Realm:** Open your Keycloak admin console and configure your operational realm.
2. **Configure Client:**
   * **Client ID:** `bff-client`
   * **Client Protocol:** `openid-connect`
   * **Access Type / Capability Config:** `confidential` (Ensure a **Client Secret** is generated under the credentials tab).
3. **Critical URL Configurations:**
   > ⚠️ **Important:** You must accurately configure both redirect fields. Misconfiguring these parameters is a common pitfall that can break your authentication loop or introduce security vulnerabilities like open redirects.
   * **Valid Redirect URIs:** `https://api.company.local/signin-oidc` *(Or your specific backend BFF redirect gateway endpoint)*
   * **Valid Post Logout Redirect URIs:** `https://api.company.local/signout-callback-oidc`

---

## 💻 Backend Configuration (`.NET 10`)

The backend service functions as your secure BFF gateway leveraging `Yarp.ReverseProxy`. 

### 1. Required NuGet Packages
Install the required infrastructure dependencies via the .NET CLI:
```bash
dotnet add package Yarp.ReverseProxy
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect