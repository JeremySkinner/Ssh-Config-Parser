using System;
using Xunit;

namespace SshConfigParser.Tests
{
    public class ParserTests
    {
        [Fact] 
        public void Parses_simple_config()
        {
            var cfg = SshConfig.ParseFile("config");
            
            cfg.Count.ShouldEqual(7);
            cfg[0].Param.ShouldEqual("ControlMaster");
            cfg[0].Value.ShouldEqual("auto");

            var result = cfg.FindByHost("tahoe1");
            result.ShouldNotBeNull();
            result.Type.ShouldEqual(NodeType.Directive);
            result.Before.ShouldEqual("");
            result.After.ShouldEqual("\n");
            result.Param.ShouldEqual("Host");
            result.Separator.ShouldEqual(" ");
            result.Value.ShouldEqual("tahoe1");

            var childConfig = result.Config;
            childConfig[0].Type.ShouldEqual(NodeType.Directive);
            childConfig[0].Before.ShouldEqual("  ");
            childConfig[0].After.ShouldEqual("\n");
            childConfig[0].Param.ShouldEqual("HostName");
            childConfig[0].Separator.ShouldEqual(" ");
            childConfig[0].Value.ShouldEqual("tahoe1.com");
            
            childConfig[1].Type.ShouldEqual(NodeType.Directive);
            childConfig[1].Before.ShouldEqual("  ");
            childConfig[1].After.ShouldEqual("\n\n");
            childConfig[1].Param.ShouldEqual("Compression");
            childConfig[1].Separator.ShouldEqual(" ");
            childConfig[1].Value.ShouldEqual("yes");
        }
    }
}