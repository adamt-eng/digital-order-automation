<?php

// Decode request data
$request_data = json_decode(file_get_contents('php://input'), true);

// Confirm request receival
http_response_code(200);

// Variables to be filled
$client_secret = "YOUR_CLIENT_SECRET_HERE";
$store_id = "YOUR_STORE_ID_HERE";
$api_token = "YOUR_API_TOKEN_HERE";
$encryption_key = "YOUR_ENCRYPTION_KEY_HERE";
$discord_webhook_url = "YOUR_DISCORD_WEBHOOK_URL_HERE";

// Filter events
if (!in_array($request_data['eventType'], ['order.created', 'order.updated']))
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
    $event_created = $request_data['eventCreated'];
    $event_id = $request_data['eventId'];
    $hmac_result = hash_hmac("sha256", "$event_created.$event_id", $client_secret, true);
    $generated_signature = base64_encode($hmac_result);
			
    $signature_header_present = false;
    $signature_valid = true;
    foreach (getallheaders() as $name => $value) 
    {
        if (strtolower($name) == "x-ecwid-webhook-signature") 
        {
            $signature_header_present = true;
			
            if ($generated_signature !== $value) 
            {
                $signature_valid = false;
                break;
            }
        }
    }
    
    if ($signature_header_present === false || $signature_valid === false)
    {
        // Send to the Discord webhook that a request contained an invalid signature or not signature at al
        curl_setopt_array($curl_init = curl_init(),
    	[
            CURLOPT_URL => $discord_webhook_url,
            CURLOPT_POST => true,
            CURLOPT_TIMEOUT => 20,
            CURLOPT_SSL_VERIFYPEER => false,
            CURLOPT_SSL_VERIFYHOST => false,
            CURLOPT_HTTPHEADER => [ 'Content-Type: application/json' ],
            CURLOPT_POSTFIELDS => json_encode([ 'content' => "Signature Invalid." ]),
        ]);
        curl_exec($curl_init);
        curl_close($curl_init);
        exit;
    }
}

$order_id = $request_data['data']["orderId"];

// Ecwid's POST request does not contain the information that the buyer has inputted when purchasing
// So we need to get the order data manually as we need the Discord User ID and other relevant information
$curl_get_order = curl_init("https://app.ecwid.com/api/v3/$store_id/orders/$order_id");
curl_setopt_array($curl_get_order,
[
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_TIMEOUT => 20,
    CURLOPT_HTTPHEADER => [ "Authorization: Bearer $api_token", "accept: application/json" ]
]);

// Decode order data
$order_data = json_decode(curl_exec($curl_get_order));
curl_close($curl_get_order);

$payment_status = $order_data->paymentStatus;
$fulfillment_status = $order_data->fulfillmentStatus;

if ($payment_status == 'PAID') 
{
    if ($fulfillment_status === "AWAITING_PROCESSING")
    {
        // Update order status

        $order_data->fulfillmentStatus = "DELIVERED";
            
        $curl_update = curl_init("https://app.ecwid.com/api/v3/$store_id/orders/$order_id");
        curl_setopt_array($curl_update,
        [
            CURLOPT_CUSTOMREQUEST => "PUT",
            CURLOPT_POSTFIELDS => json_encode($order_data),
            CURLOPT_HTTPHEADER => [ "Authorization: Bearer $api_token", "accept: application/json", "content-type: application/json" ]
        ]);
    
        curl_exec($curl_update);
        curl_close($curl_update);
    }
}

$ip_address = $order_data->ipAddress;
$total = strval($order_data->total);
$email = $order_data->email;
$payment_method = $order_data->paymentMethod ?? "N/A";

$items = $order_data->items[0];
$discord_user_id = $items->selectedOptions[0]->value;

// Get country based on IP address
$curl_country = curl_init("http://ip-api.com/json/$ip_address?fields=country");
curl_setopt($curl_country, CURLOPT_RETURNTRANSFER, true);
$country = json_decode(curl_exec($curl_country))->country;
curl_close($curl_country);

// Send order information to Discord webhook
$curl_send = curl_init();
curl_setopt_array($curl_send, [ CURLOPT_URL => $discord_webhook_url, CURLOPT_POST => true, CURLOPT_POSTFIELDS => json_encode(
[ 
    "embeds" =>
    [
        [ 
            "type" => "rich",
            "color" => "682401",
            "fields" => 
            [
                [ "name" => "[$payment_status - {$total}€]", "value" => "[$order_id](https://my.ecwid.com/store/$store_id#order:id=$order_id)", "inline" => false ], 
                [ "name" => "Email Address", "value" => $email, "inline" => false ], 
                [ "name" => "IP Address", "value" => "$ip_address - $country", "inline" => false ], 
                [ "name" => "Discord User ID", "value" => $discord_user_id, "inline" => true ]
            ] 
        ]
    ]
]), CURLOPT_HTTPHEADER => [ "Content-Type: application/json" ]]);

curl_exec($curl_send);
curl_close($curl_send);

?>