using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;

namespace D2UserHost
{
    [RunInstaller(true)]
    public partial class D2UserHostInstaller : System.Configuration.Install.Installer
    {
        public D2UserHostInstaller()
        {
            InitializeComponent();
        }
    }
}
