namespace SendAlarmNotifications_1
{
	using System;
	using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Web;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Net.Messages;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        private GetUserInfoResponseMessage userInfoResponseMessage;
        private List<string> allTelephoneNumbers;
        private AlarmEventMessage alarmDetails;

        private string ipAddress;
        private string port;
        private string username;
        private string password;

        /// <summary>
        /// The script entry point.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
        public void Run(IEngine engine)
        {
            try
            {
                Initialize(engine);

                var alarmInfo = engine.GetScriptParam(65006).Value;
                var alarm = CorrelatedAlarmInfo.FromCorrelatedInfo(alarmInfo);

                RetrieveAlarmDetails(engine, alarm);

                if (alarmDetails == null)
                {
                    engine.GenerateInformation($"Couldn't retrieve the alarm details");
                    return;
                }

                RetrieveUserInfo(engine);
                FilterOutTelephoneNumbers();
                SendGetRequest(engine);
            }
            catch (Exception ex)
            {
                engine.GenerateInformation($"EXCEPTION:{ex.Message}");
            }
        }

        private void Initialize(IEngine engine)
        {
            ipAddress = engine.GetScriptParam("IP Address").Value;
            port = engine.GetScriptParam("Port").Value;
            username = engine.GetScriptParam("Username").Value;
            password = engine.GetScriptParam("Password").Value;

            if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(port) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                engine.GenerateInformation($"DEBUG: One of the input parameters is not valid");
                return;
            }
        }

        private void RetrieveAlarmDetails(IEngine engine, CorrelatedAlarmInfo alarm, int maxRetries = 10)
        {
            int retries = 0;

            while (retries < maxRetries && alarmDetails == null)
            {
                Thread.Sleep(250);

                GetAlarmDetailsMessage alarmMsg = new GetAlarmDetailsMessage(-1, -1, new int[] { alarm.AlarmId });
                var response = engine.SendSLNetMessage(alarmMsg);

                alarmDetails = response.Length > 0 ? response[0] as AlarmEventMessage : null;

                retries++;
            }
        }

        private void RetrieveUserInfo(IEngine engine)
        {
            DMSMessage dmsMessage = engine.SendSLNetSingleResponseMessage(new GetInfoMessage(InfoType.SecurityInfo));

            userInfoResponseMessage = dmsMessage as GetUserInfoResponseMessage;
        }

        private void FilterOutTelephoneNumbers()
        {
            allTelephoneNumbers = new List<string>();
            foreach (var user in userInfoResponseMessage.Users)
            {
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    allTelephoneNumbers.Add(user.PhoneNumber);
                }
            }
        }

        private string ComposeMessage()
        {
            string message = $@"{alarmDetails.Severity} alarm: {alarmDetails.ElementName} - Description: {alarmDetails.ParameterName} - Value: {alarmDetails.DisplayValue} - Time: {alarmDetails.RootTime}";

            return HttpUtility.UrlEncode(message, Encoding.UTF8);
        }

        private string ConcatenateTelephoneNumbers()
        {
            var concatenatedTelephoneNumbers = string.Join(" ", allTelephoneNumbers);

            return HttpUtility.UrlEncode(concatenatedTelephoneNumbers, Encoding.UTF8);
        }

        private string ComposeUrl()
        {
            string telephoneNumbers = ConcatenateTelephoneNumbers();
            string message = ComposeMessage();

            return $"http://{ipAddress}:{port}/cgi-bin/sendsms?username={username}&password={password}&to={telephoneNumbers}&text={message}";
        }

        private void SendGetRequest(IEngine engine)
        {
            using (var client = new HttpClient())
            {
                var composedUri = ComposeUrl();

                var endPoint = new Uri(composedUri);
                var result = client.GetAsync(endPoint).Result;
                var stringResult = result.Content.ReadAsStringAsync().Result;

                if (!stringResult.Contains("0: Accepted for delivery"))
                {
                    engine.GenerateInformation($"Send Alarm Notifications | Request URL: {composedUri} Response: {stringResult}");
                }
            }
        }
    }
}