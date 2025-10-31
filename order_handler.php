<?php

// Import from config.php ECWID_CLIENT_SECRET and ECWID_API_TOKEN
require_once __DIR__ . '/config.php';

// Decode request data
$requestData = json_decode(file_get_contents('php://input'), true);

http_response_code(200);

$storeId = "YOUR_STORE_ID_HERE";
$discordWebhookUrl = "YOUR_DISCORD_WEBHOOK_URL_HERE";

// Filter events
if (!in_array($requestData['eventType'], ['order.created', 'order.updated']))
{
    exit;
}

/**
 * Minimal cURL wrapper.
 * Returns response body string on success, or false on error.
 */
function sendCurl($url, array $options) {
    $ch = curl_init($url);
    curl_setopt_array($ch, $options + [
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_TIMEOUT => 20,
    ]);
    $response = curl_exec($ch);
    if ($response === false) {
        error_log('cURL error (' . $url . '): ' . curl_error($ch));
    }
    curl_close($ch);
    return $response;
}

// Check if the request is legitimate
{
    // Add getallheaders() if it doesn't exist
    if (!function_exists('getallheaders')) 
    {
        function getallheaders()
        {
            $headers = [];
            foreach ($_SERVER as $name => $value)
            {
                if (substr($name, 0, 5) == 'HTTP_') 
                {
                    $headers[str_replace(' ', '-', ucwords(strtolower(str_replace('_', ' ', substr($name, 5)))))] = $value;
                }
            }
            return $headers;
        }
    }
    
    // Verify signature
    $eventCreated = $requestData['eventCreated'];
    $eventId = $requestData['eventId'];
    $hmacResult = hash_hmac("sha256", "$eventCreated.$eventId", ECWID_CLIENT_SECRET, true);
    $generatedSignature = base64_encode($hmacResult);
			
    $signatureHeaderPresent = false;
    $signatureValid = true;
    foreach (getallheaders() as $name => $value) 
    {
        if (strtolower($name) == "x-ecwid-webhook-signature") 
        {
            $signatureHeaderPresent = true;
			
            if ($generatedSignature !== $value) 
            {
                $signatureValid = false;
                break;
            }
        }
    }
    
    if ($signatureHeaderPresent === false || $signatureValid === false)
    {
        sendCurl($discordWebhookUrl, [
            CURLOPT_POST => true,
            CURLOPT_SSL_VERIFYPEER => false,
            CURLOPT_SSL_VERIFYHOST => false,
            CURLOPT_HTTPHEADER => ['Content-Type: application/json'],
            CURLOPT_POSTFIELDS => json_encode(['content' => 'Signature Invalid.']),
        ]);
        exit;
    }
}

$orderId = $requestData['data']["orderId"];

// Ecwid's POST request does not contain the information that the buyer has inputted when purchasing
// So we need to get the order data manually as we need the Discord User ID and other relevant information
$response = sendCurl("https://app.ecwid.com/api/v3/$storeId/orders/$orderId", [
    CURLOPT_HTTPHEADER => [
        'Authorization: Bearer ' . ECWID_API_TOKEN,
        'Accept: application/json',
    ],
]);

if ($response === false) {
    exit;
}

// Decode order data
$orderData = json_decode($response);

$paymentStatus = $orderData->paymentStatus;
$fulfillmentStatus = $orderData->fulfillmentStatus;

if ($paymentStatus == 'PAID') 
{
    if ($fulfillmentStatus === "AWAITING_PROCESSING")
    {
        // Update order status

        $orderData->fulfillmentStatus = "DELIVERED";
            
        $response = sendCurl("https://app.ecwid.com/api/v3/$storeId/orders/$orderId", [
            CURLOPT_CUSTOMREQUEST => 'PUT',
            CURLOPT_POSTFIELDS => json_encode($orderData),
            CURLOPT_HTTPHEADER => [
                'Authorization: Bearer ' . ECWID_API_TOKEN,
                'Accept: application/json',
                'Content-Type: application/json',
            ],
        ]);
    
        if ($response === false) {
            exit;
        }
    }
}

$ipAddress = $orderData->ipAddress;
$total = strval($orderData->total);
$email = $orderData->email;
$paymentMethod = $orderData->paymentMethod ?? "N/A";

$items = $orderData->items[0];
$discordUserId = $items->selectedOptions[0]->value;

// Send order information to Discord webhook
$ch = curl_init();
$payload = json_encode(
[ 
    "embeds" =>
    [
        [ 
            "type" => "rich",
            "color" => "682401",
            "fields" => 
            [
                [ "name" => "[$paymentStatus - {$total}€]", "value" => "[$orderId](https://my.ecwid.com/store/$storeId#order:id=$orderId)", "inline" => false ], 
                [ "name" => "Email Address", "value" => $email, "inline" => false ], 
                [ "name" => "IP Address", "value" => $ipAddress, "inline" => false ], 
                [ "name" => "Discord User ID", "value" => $discordUserId, "inline" => true ]
            ] 
        ]
    ]
]);

sendCurl($discordWebhookUrl, [
    CURLOPT_POST => true,
    CURLOPT_HTTPHEADER => ['Content-Type: application/json'],
    CURLOPT_POSTFIELDS => $payload,
]);


?>