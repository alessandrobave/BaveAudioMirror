using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaveAudioMirror
{
    internal class userSettings : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        [DefaultSettingValue("False")]
        public string lastOutPutDevice
        {
            get { return (string)this["lastOutPutDevice"]; }
            set { this["lastOutPutDevice"] = value; }
        }
    }
}
