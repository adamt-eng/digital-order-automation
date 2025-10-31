# Digital Product Order Fulfillment System

## Introduction

Most e-commerce platforms allow the seller to set a Webhook URL to receive notifications regarding their store, such as for new orders. In this project, I've utilized this feature to not only get a notification for the order but to also automatically grant the buyer access to the digital product they purchased.

### Problem and Solution

**Problem:** You start selling a digital product on an e-commerce platform—let’s say the product is a very special text file containing top-secret information. You've uploaded this file into a private channel on your Discord server where you promote your products. To access the product, buyers need a special role called 'Customer.' 

Currently, each time a buyer makes a purchase, you receive an order notification with the buyer's Discord User ID. You then manually tag the buyer in your server and assign them the 'Customer' role. While this works, it’s time-consuming. If multiple buyers purchase your product simultaneously, you could struggle to keep up, leading to delays in granting access.

**Solution:** This project automates the process. When you receive the order notification, a Discord bot will listen for it, read the order data (including the buyer's Discord User ID), and instantly grant the buyer the 'Customer' role in your Discord server. This system saves you time, provides instant delivery to buyers, and allows potential buyers to purchase at any time without requiring you to be available.

## Overview

The system consists of two main components:
1. A PHP script that handles the initial order POST notification.
2. A C# application running continuously on a VPS that interacts with the Discord server to grant the buyer the 'Customer' role.

## Process Workflow

### 1. Order Placement and Initial Processing (PHP)

When a user purchases the product through your website, the following steps occur:

- **Webhook Trigger**: A POST notification is sent to the webhook URL, `order_handler.php`.

> The e-commerce site this system is specifically adapted to is Ecwid so that is what I will be using for examples.

#### Webhook Body Examples (from [Ecwid](https://api-docs.ecwid.com/reference/webhooks-body-examples))

##### Order Created

```json
{
  "eventId": 1234567890,
  "eventType": "order.created",
  "entityId": 9876543210,
  "data": {
    "order": {
      "id": 12345,
      "orderNumber": 54321,
      "total": 99.99,
      "subtotal": 79.99,
      "tax": 10.00,
      "email": "customer@example.com",
      "orderStatus": "AWAITING_PROCESSING",
      "items": [
        {
          "id": 1,
          "productId": 1001,
          "quantity": 2,
          "price": 39.99
        }
      ]
    }
  }
}
```

##### Order Updated

```json
{
  "eventId":"80aece08-40e8-4145-8764-6c2f0d38678",
  "eventCreated":1234567,
  "storeId":1003,
  "entityId":450012387,
  "eventType":"order.updated",
  "data":{
    "orderId":"B8HGD",
    "oldPaymentStatus":"PAID",
    "newPaymentStatus":"PAID",
    "oldFulfillmentStatus":"PROCESSING",
    "newFulfillmentStatus":"SHIPPED"
  }
}
```

- **PHP Script Execution**: The PHP script processes the order details and sends them in the structure of a [Discord embed](https://discordjs.guide/popular-topics/embeds.html#embed-preview) to a [Discord webhook](https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks) that was created in a channel I've named `#orders`.

###### Discord Embed Example

![Embed Example](Media/Embed%20Example.png)

### 2. Discord Embed Handling (C#)

The C# application listens for messages sent in the `#orders` channel by the Discord webhook or by the admin (for a specific reason mentioned later). When the message is received, the following steps occur:

- **Discord Embed Parsing**: The application parses the embed sent by the PHP script, extracting necessary information such as the order ID, payment status, and Discord User ID.

- **Role Assignment**: If the order's payment status is "PAID", the application assigns the 'Customer' role to the user in the Discord server. If the payment status is anything else, it indicates that the buyer has refunded, and instead of assigning the 'Customer' role, it is revoked.

###### Process Showcase

![Process Showcase](Media/Process%20Showcase.gif)