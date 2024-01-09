using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendAlarmNotifications_1
{
    internal class CorrelatedAlarmInfo
    {
        #region Constructors
        private CorrelatedAlarmInfo()
        {
        }
        #endregion

        #region Public properties
        public int AlarmId { get; set; }

        public AlarmProperty[] AlarmProperties { get; set; }

        public DateTime AlarmTime { get; set; }

        public int AlarmType { get; set; }

        public string AlarmValue { get; set; }

        public int AmountProperties { get; set; }

        public int DmaID { get; set; }

        public int ElementID { get; set; }

        public int ElementRCA { get; set; }

        public string ImpactedServices { get; set; }

        public string Owner { get; set; }

        public int ParameterID { get; set; }

        public string ParameterIndex { get; set; }

        public int ParameterRCA { get; set; }

        public int PrevAlarmID { get; set; }

        public int RootAlarmID { get; set; }

        public int ServiceRCA { get; set; }

        public int Severity { get; set; }

        public int SeverityRange { get; set; }

        public int SourceID { get; set; }

        public int Status { get; set; }

        public int UserStatus { get; set; }
        #endregion

        public static CorrelatedAlarmInfo FromCorrelatedInfo(string alarmInfo)
        {
            if (string.IsNullOrWhiteSpace(alarmInfo))
                throw new ArgumentNullException(nameof(alarmInfo));
            var parts = alarmInfo.Split('|');
            if (parts.Length < 21)
                throw new Exception("Invalid number of correlated alarm components");

            var correlatedAlarm = new CorrelatedAlarmInfo
            {
                AlarmId = Tools.ToInt32(parts[0]),
                DmaID = Tools.ToInt32(parts[1]),
                ElementID = Tools.ToInt32(parts[2]),
                ParameterID = Tools.ToInt32(parts[3]),
                ParameterIndex = parts[4],
                RootAlarmID = Tools.ToInt32(parts[5]),
                PrevAlarmID = Tools.ToInt32(parts[6]),
                Severity = Tools.ToInt32(parts[7]),
                AlarmType = Tools.ToInt32(parts[8]),
                Status = Tools.ToInt32(parts[9]),
                AlarmValue = parts[10],
                AlarmTime = DateTime.Parse(parts[11]),
                ServiceRCA = Tools.ToInt32(parts[12]),
                ElementRCA = Tools.ToInt32(parts[13]),
                ParameterRCA = Tools.ToInt32(parts[14]),
                SeverityRange = Tools.ToInt32(parts[15]),
                SourceID = Tools.ToInt32(parts[16]),
                UserStatus = Tools.ToInt32(parts[17]),
                Owner = parts[18],
                ImpactedServices = parts[19],
                AmountProperties = Tools.ToInt32(parts[20]),
            };

            List<AlarmProperty> properties = new List<AlarmProperty>();
            for (int i = 0; i < correlatedAlarm.AmountProperties; i++)
            {
                properties.Add(
                    new AlarmProperty() { PropertyName = parts[21 + i * 2], PropertyValue = parts[21 + i * 2 + 1], });
            }

            correlatedAlarm.AlarmProperties = properties.ToArray();
            return correlatedAlarm;
        }

        public override string ToString()
        {
            return $"CorrelatedAlarmInfo {AlarmValue} on element {ElementID}/{DmaID} and parameter {ParameterID} ";
        }

        #region Nested Classes
        internal class AlarmProperty
        {
            #region Public properties
            public string PropertyName { get; set; }

            public string PropertyValue { get; set; }
            #endregion
        }
        #endregion
    }
}
