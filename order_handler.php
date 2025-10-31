<?php

// Decode request data
$requestData = json_decode(file_get_contents('php://input'), true);

// Confirm request receival
http_response_code(200);

// Variables to be filled
$clientSecret = "YOUR_CLIENT_SECRET_HERE";
$storeId = "YOUR_STORE_ID_HERE";
$apiToken = "YOUR_API_TOKEN_HERE";
$encryptionKey = "YOUR_ENCRYPTION_KEY_HERE";
$discordWebhookUrl = "YOUR_DISCORD_WEBHOOK_URL_HERE";

// Filter events
if (!in_array($requestData['eventType'], ['order.created', 'order.updated']))
{
    exit;
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
    $hmacResult = hash_hmac("sha256", "$eventCreated.$eventId", $clientSecret, true);
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
        // Send to the Discord webhook that a request contained an invalid signature or not signature at al
        curl_setopt_array($ch = curl_init(),
    	[
            CURLOPT_URL => $discordWebhookUrl,
            CURLOPT_POST => true,
            CURLOPT_TIMEOUT => 20,
            CURLOPT_SSL_VERIFYPEER => false,
            CURLOPT_SSL_VERIFYHOST => false,
            CURLOPT_HTTPHEADER => [ 'Content-Type: application/json' ],
            CURLOPT_POSTFIELDS => json_encode([ 'content' => "Signature Invalid." ]),
        ]);
        curl_exec($ch);
        curl_close($ch);
        exit;
    }
}

$orderId = $requestData['data']["orderId"];

// Ecwid's POST request does not contain the information that the buyer has inputted when purchasing
// So we need to get the order data manually as we need the Discord User ID and other relevant information
$ch = curl_init("https://app.ecwid.com/api/v3/$storeId/orders/$orderId");
curl_setopt_array($ch,
[
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_TIMEOUT => 20,
    CURLOPT_HTTPHEADER => [ "Authorization: Bearer $apiToken", "accept: application/json" ]
]);

// Decode order data
$orderData = json_decode(curl_exec($ch));
curl_close($ch);

$paymentStatus = $orderData->paymentStatus;
$fulfillmentStatus = $orderData->fulfillmentStatus;

if ($paymentStatus == 'PAID') 
{
    if ($fulfillmentStatus === "AWAITING_PROCESSING")
    {
        // Update order status

        $orderData->fulfillmentStatus = "DELIVERED";
            
        $ch = curl_init("https://app.ecwid.com/api/v3/$storeId/orders/$orderId");
        curl_setopt_array($ch,
        [
            CURLOPT_CUSTOMREQUEST => "PUT",
            CURLOPT_POSTFIELDS => json_encode($orderData),
            CURLOPT_HTTPHEADER => [ "Authorization: Bearer $apiToken", "accept: application/json", "content-type: application/json" ]
        ]);
    
        curl_exec($ch);
        curl_close($ch);
    }
}

$ipAddress = $orderData->ipAddress;
$total = strval($orderData->total);
$email = $orderData->email;
$paymentMethod = $orderData->paymentMethod ?? "N/A";

$items = $orderData->items[0];
$discordUserId = $items->selectedOptions[0]->value;

// Get country based on IP address
$ch = curl_init("http://ip-api.com/json/$ipAddress?fields=country");
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
$country = json_decode(curl_exec($ch))->country;
curl_close($ch);

// Send order information to Discord webhook
$ch = curl_init();
curl_setopt_array($ch, [ CURLOPT_URL => $discordWebhookUrl, CURLOPT_POST => true, CURLOPT_POSTFIELDS => json_encode(
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
                [ "name" => "IP Address", "value" => "$ipAddress - $country", "inline" => false ], 
                [ "name" => "Discord User ID", "value" => $discordUserId, "inline" => true ]
            ] 
        ]
    ]
]), CURLOPT_HTTPHEADER => [ "Content-Type: application/json" ]]);

curl_exec($ch);
curl_close($ch);

?>