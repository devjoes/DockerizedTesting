using System;
using System.Collections.Generic;
using System.Text;

namespace DockerizedTesting.MsSql
{
    public class MsSqlFixtureOptions : FixtureOptions
    {
        public string Image { get; set; } = "mcr.microsoft.com/mssql/server:latest";
        public string SaPassword { get; set; } = "D0cK3rIz3d_T3sting!!";
        public bool AcceptEula { get; set; } = true;
        public MsSqlProduct Product { get; set; } = MsSqlProduct.Developer;

        public override int DelayMs => 2000;
    }
}
