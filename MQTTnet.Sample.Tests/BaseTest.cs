using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.Sample.Tests
{
    public class BaseTest
    {
        #region Fields

        protected string ServerAddress => "localhost";

        protected string Topic => "andy840119/iot";

        protected MqttQualityOfServiceLevel ConnectQuality => MqttQualityOfServiceLevel.AtLeastOnce;

        #endregion

        #region Equality

        /// <summary>
        /// Check Bytes array are equal.
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="b1"></param>
        /// <returns></returns>
        public bool Equality(byte[] a1, byte[] b1)
        {
           int i;
           if (a1.Length == b1.Length)
           {
              i = 0;
              while (i < a1.Length && (a1[i]==b1[i])) //Earlier it was a1[i]!=b1[i]
              {
                  i++;
              }
              if (i == a1.Length)
              {
                  return true;
              }
           }

           return false;
        }

        #endregion
    }
}
