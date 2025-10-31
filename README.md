# Digital Product Order Fulfillment System

## Introduction

Many e-commerce platforms allow sellers to define a **webhook URL** that receives real-time notifications when store events occur, such as new orders.
This project uses that capability to **automate digital product delivery** via Discord.

When a customer completes a purchase, the system detects the order, reads the buyer’s Discord User ID, and automatically grants them access to protected channels by assigning the **Customer** role.

This replaces the manual role-assignment process and enables 24/7 automatic order fulfillment.

---

## Problem and Solution

### Problem

Digital sellers on platforms like Ecwid must manually grant buyers access to digital content after each purchase.
When multiple orders arrive simultaneously, this manual process causes slow delivery and inconsistent customer experience.

### Solution

This system eliminates manual intervention.
A PHP webhook handler receives and validates Ecwid order events. It forwards sanitized order details to a private Discord channel as an **embed message**.
A **C# application (Discord bot)** monitors that channel, parses each embed, and automatically assigns or removes roles in the Discord server depending on payment status.

---

## System Overview

The system has two main components:

1. **PHP Order Handler** – Listens for incoming Ecwid webhooks, verifies authenticity, and sends formatted order data to a Discord channel.
2. **C# Discord Application** – Runs persistently, monitors the orders channel, and manages customer roles automatically.

---

## Workflow Summary

### 1. Order Placement and PHP Processing

1. **Webhook Triggered**
   When a customer completes a purchase, Ecwid sends a `POST` request to your webhook endpoint (`order_handler.php`).

2. **Signature Verification**
   The script verifies the HMAC signature to ensure the event came from Ecwid.

3. **Order Retrieval and Handling**
   The script fetches full order details from Ecwid’s REST API, updates fulfillment status if needed, and creates or deletes user records based on payment state.

4. **Discord Notification**
   A structured Discord embed is sent to a webhook in the `#orders` channel with details like:

   * Order ID
   * Payment Status
   * Email
   * Discord User ID

**Example Embed**

![Embed Example](media/Embed%20Example.png)

---

### 2. Discord Bot Processing (C#)

1. **Embed Detection**
   The bot listens for new messages in the `#orders` channel. When it detects a webhook embed, it extracts the relevant fields.

2. **Order Parsing**
   The bot reads the payment status and Discord User ID from the embed.

3. **Role Management**

   * If `PAID`, the bot adds the **Customer** role.
   * If refunded or canceled, the bot removes the role.

4. **Logging**
   Every action is logged to the console with timestamps and severity levels for traceability.

**Process Example**

![Process Showcase](media/Process%20Showcase.gif)

---

## Environment & Webhook Configuration

The system uses **environment variables** for the C# Discord bot and simple **constant definitions** for the PHP webhook.
This keeps credentials secure, portable, and easy to deploy across environments.

---

### 1. C# Discord Application

The C# bot no longer depends on JSON configuration files.
All required settings are read from **environment variables**.

#### Required Variables

| Variable                                                           | Description                                      |
| ------------------------------------------------------------------ | ------------------------------------------------ |
| `DISCORD_APP_TOKEN`                                                | Discord bot token                                |
| `GUILD_ID`                                                         | Target Discord server (guild) ID                 |
| `ORDERS_CHANNEL_ID`                                                | Channel where order embeds are posted            |
| `CUSTOMER_ROLE_ID`                                                 | Role to assign or remove based on payment status |

#### Example `.env` (development)

```bash
DISCORD_APP_TOKEN=your_discord_bot_token_here
GUILD_ID=123456789012345678
ORDERS_CHANNEL_ID=234567890123456789
CUSTOMER_ROLE_ID=345678901234567890
```

---

### 2. PHP Webhook Setup

1. Place the `ecwid_webhook_handler.php` file on your server, for example:

   ```
   https://yourdomain.com/webhooks/ecwid_webhook_handler.php
   ```

2. Create or edit `config.php` in the same directory and define the following constants:

   ```php
   define('ECWID_API_TOKEN', 'your_ecwid_api_token');
   define('ECWID_CLIENT_SECRET', 'your_ecwid_client_secret');
   define('STORE_ID', 'your_store_id');
   define('DISCORD_WEBHOOK_URL', 'https://discord.com/api/webhooks/...'); 
   ```

3. In your **Ecwid admin panel**, navigate to:
   `Settings → API → Webhooks → Add Webhook`
   and set the webhook URL to your deployed PHP script.

---

This configuration allows both components to operate independently:

* **PHP** handles Ecwid’s webhook requests and sends order notifications to Discord.
* **C#** monitors the Discord channel, processes those messages, and manages user roles automatically.
