$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add("http://localhost:5099/")
$listener.Start()
Write-Host "Mock API listening on http://localhost:5099"

while ($listener.IsListening) {
    $ctx = $listener.GetContext()
    $json = @'
{
  "ScanEvents": [
    {"EventId":7,"ParcelId":5001,"Type":"PICKUP","CreatedDateTimeUtc":"2026-02-21T10:00:00Z","StatusCode":"","Device":{"DeviceTransactionId":1,"DeviceId":101},"User":{"UserId":"NC1001","CarrierId":"NC","RunId":"100"}},
    {"EventId":8,"ParcelId":5001,"Type":"STATUS","CreatedDateTimeUtc":"2026-02-21T11:00:00Z","StatusCode":"IN_TRANSIT","Device":{"DeviceTransactionId":2,"DeviceId":102},"User":{"UserId":"NC1002","CarrierId":"NC","RunId":"101"}},
    {"EventId":9,"ParcelId":5002,"Type":"PICKUP","CreatedDateTimeUtc":"2026-02-21T12:00:00Z","StatusCode":"","Device":{"DeviceTransactionId":3,"DeviceId":103},"User":{"UserId":"PH2001","CarrierId":"PH","RunId":"200"}},
    {"EventId":10,"ParcelId":5001,"Type":"DELIVERY","CreatedDateTimeUtc":"2026-02-21T13:00:00Z","StatusCode":"","Device":{"DeviceTransactionId":4,"DeviceId":101},"User":{"UserId":"NC1001","CarrierId":"NC","RunId":"100"}},
    {"EventId":11,"ParcelId":5002,"Type":"DELIVERY","CreatedDateTimeUtc":"2026-02-21T14:00:00Z","StatusCode":"","Device":{"DeviceTransactionId":5,"DeviceId":103},"User":{"UserId":"PH2001","CarrierId":"PH","RunId":"200"}}
  ]
}
'@
    $buffer = [System.Text.Encoding]::UTF8.GetBytes($json)
    $ctx.Response.ContentType = "application/json"
    $ctx.Response.OutputStream.Write($buffer, 0, $buffer.Length)
    $ctx.Response.Close()
    Write-Host "Served request: $($ctx.Request.Url)"
}