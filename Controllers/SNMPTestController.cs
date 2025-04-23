//using Microsoft.AspNetCore.Mvc;
//using Lextm.SharpSnmpLib;
//using Lextm.SharpSnmpLib.Messaging;
//using System.Net; // Required for IPEndPoint
//using System.Threading.Tasks; // For async/await

//public class SnmpTestController : Controller
//{
//    // Hardcoded example - replace with config or user input later
//    private const string TargetIp = "127.0.0.1"; 
//    private const string Community = "123456"; 
//    private const int Port = 161;
//    private static readonly ObjectIdentifier SysDescrOid = new ObjectIdentifier("1.3.6.1.2.1.1.1.0");

//    public async Task<IActionResult> GetData()
//    {
//        string resultData = "Error fetching data."; // Default message
//        try
//        {
//            // Prepare variables for the GET request
//            var variables = new List<Variable> { new Variable(SysDescrOid) };

//            // Send the SNMP GET request (using v2c)
//            // Note: Discovery is optional but helps ensure compatibility
//            var discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
//            var report = await discovery.GetResponseAsync(new IPEndPoint(IPAddress.Parse(TargetIp), Port), new OctetString(Community), variables, TimeSpan.FromSeconds(5));


//            // Check if a response was received and if there are variables
//            if (report != null && report.Pdu().Variables.Count > 0)
//            {
//                var variable = report.Pdu().Variables[0];
//                if (variable.Data is OctetString data) // Check if data is OctetString
//                {
//                    resultData = data.ToString(); // Get the string representation
//                }
//                else
//                {
//                    resultData = $"Received data, but not an OctetString: {variable.Data.ToString()}";
//                }

//            }
//            else
//            {
//                resultData = "No response or no variables received from device.";
//                // You might want more specific error handling based on Pdu().ErrorStatus if available
//                if (report?.Pdu()?.ErrorStatus != ErrorCode.NoError)
//                {
//                    resultData += $" SNMP Error: {report.Pdu().ErrorStatus}";
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            // Log the exception details properly in a real app
//            resultData = $"An exception occurred: {ex.Message}";
//        }

//        ViewData["SnmpResult"] = resultData;
//        return View(); // Assumes you have a Views/SnmpTest/GetData.cshtml
//    }
//}