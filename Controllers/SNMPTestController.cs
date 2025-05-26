using Microsoft.AspNetCore.Mvc;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security; // Needed for VersionCode
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic; // Needed for List<Variable>
using System; // Needed for Exception
using System.Threading;

public class SnmpTestController : Controller
{
    //       '127.0.0.1' means "this same computer". Only use if the SNMP device is running on the same machine as your web app.
    private const string TargetIp = "42.96.17.217";

    //       "public" is a common default for read-only access, but might be different.
    private const string Community = "123456";

    // Standard SNMP port, usually don't need to change this.
    private const int Port = 161;

    // Timeout in milliseconds (how long to wait for an answer). 5 seconds is usually okay to start.
    private const int TimeoutMilliseconds = 5000;

    // --- The OID We Want to Ask For ---
    private static readonly ObjectIdentifier SysDescrOid = new ObjectIdentifier("1.3.6.1.2.1.1.1.0");

    // --- The Action Method ---
    public async Task<IActionResult> GetData()
    {
        string resultMessage = "Starting SNMP request...";

        // *** Use CancellationTokenSource for timeout ***
        // Create a source that can issue cancellation tokens
        using var cts = new CancellationTokenSource();
        try
        {
            // Tell the source to automatically cancel after our desired timeout
            cts.CancelAfter(TimeoutMilliseconds);

            // 1. Prepare the list of questions (OIDs).
            var variablesToAskFor = new List<Variable> { new Variable(SysDescrOid) };

            // 2. Prepare the device address and community string
            var deviceEndpoint = new IPEndPoint(IPAddress.Parse(TargetIp), Port);
            var communityString = new OctetString(Community);

            // 3. Send the request using the #SNMP library
            //    Provide the token from our CancellationTokenSource.
            //    If the timeout period passes, cts.CancelAfter() triggers the token,
            //    and the GetAsync method should throw an OperationCanceledException.
            IList<Variable> responseVariables = await Messenger.GetAsync(
                VersionCode.V2,
                deviceEndpoint,
                communityString,
                variablesToAskFor,
                cts.Token 
            );

            // 4. Check the response (same as before)
            if (responseVariables != null && responseVariables.Count > 0)
            {
                Variable receivedVariable = responseVariables[0];
                if (receivedVariable.Data != null && receivedVariable.Data.TypeCode != SnmpType.Null)
                {
                    resultMessage = $"Success! Received: {receivedVariable.Data.ToString()}";
                }
                else
                {
                    resultMessage = $"Device responded, but gave no data or an error for OID {SysDescrOid}. TypeCode: {receivedVariable.Data?.TypeCode}";
                }
            }
            else
            {
                resultMessage = "No response received (check device, IP, community, firewalls). Potential timeout before response.";
            }
        }
        // *** Catch the specific exception for cancellation/timeout ***
        catch (OperationCanceledException)
        {
            // This exception is expected if the cts.CancelAfter() time limit was hit
            resultMessage = $"The SNMP request timed out after {TimeoutMilliseconds / 1000} seconds.";
        }
        catch (Exception ex)
        {
            // Handle other errors (network, etc.)
            resultMessage = $"An error occurred: {ex.GetType().Name} - {ex.Message}";
            if (ex is System.Net.Sockets.SocketException)
            {
                resultMessage += $"\n(Could not reach the device at {TargetIp}:{Port}. Is the IP correct? Is the device on? Is there a firewall blocking?)";
            }
        }
        // Note: No finally block needed for cts because 'using var cts = ...' handles disposal

        ViewData["ResultMessage"] = resultMessage;
        return View();
    }
}