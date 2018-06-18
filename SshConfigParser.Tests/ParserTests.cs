using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace SshConfigParser.Tests
{
    // Tests ported from https://github.com/dotnil/ssh-config
    public class ParserTests
    {
        [Fact]
        public void Parses_simple_config()
        {
            var cfg = SshConfig.ParseFile("config");

            cfg.Count.ShouldEqual(7);
            cfg[0].Param.ShouldEqual("ControlMaster");
            cfg[0].Value.ShouldEqual("auto");

            var result = cfg.FindNodeByHost("tahoe1");
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

        [Fact]
        public void Parses_config_with_parameters_and_values_separated_by_equal()
        {
            var cfg = SshConfig.ParseFile("config04");

            var n = cfg[0];

            n.Type.ShouldEqual(NodeType.Directive);
            n.Before.ShouldEqual("");
            n.After.ShouldEqual("\n");
            n.Param.ShouldEqual("Host");
            n.Value.ShouldEqual("tahoe4");

            var c1 = n.Config[0];
            var c2 = n.Config[1];

            c1.Type.ShouldEqual(NodeType.Directive);
            c1.Before.ShouldEqual("  ");
            c1.After.ShouldEqual("\n");
            c1.Param.ShouldEqual("HostName");
            c1.Separator.ShouldEqual("=");
            c1.Value.ShouldEqual("tahoe4.com");

            c2.Type.ShouldEqual(NodeType.Directive);
            c2.Before.ShouldEqual("  ");
            c2.After.ShouldEqual("\n");
            c2.Param.ShouldEqual("User");
            c2.Separator.ShouldEqual("=");
            c2.Value.ShouldEqual("keanu");
        }

        [Fact]
        public void Parses_comments()
        {
            var cfg = SshConfig.ParseFile("config05");
            cfg[0].Type.ShouldEqual(NodeType.Comment);
            cfg[0].Content.ShouldEqual("# I'd like to travel to lake tahoe.");

            // The comments goes with sections. So the structure is not the way it seems.
            cfg[1].Config[1].Type.ShouldEqual(NodeType.Comment);
            cfg[1].Config[1].Content.ShouldEqual("# or whatever place it is.");
        }

        [Fact]
        public void Parses_multiple_identityFiles()
        {
            var cfg = SshConfig.ParseFile("config06");

            cfg[1].Param.ShouldEqual("IdentityFile");
            cfg[1].Value.ShouldEqual("~/.ssh/ids/%h/%r/id_rsa");

            cfg[2].Param.ShouldEqual("IdentityFile");
            cfg[2].Value.ShouldEqual("~/.ssh/ids/%h/id_rsa");

            cfg[3].Param.ShouldEqual("IdentityFile");
            cfg[3].Value.ShouldEqual("~/.ssh/id_rsa");
        }

        [Fact]
        public void Parses_IdentityFile_with_spaces()
        {
            var cfg = SshConfig.ParseFile("config07");

            cfg[0].Param.ShouldEqual("IdentityFile");
            cfg[0].Value.ShouldEqual("C:\\Users\\fname lname\\.ssh\\id_rsa");

            cfg[1].Param.ShouldEqual("IdentityFile");
            cfg[1].Value.ShouldEqual("C:\\Users\\fname lname\\.ssh\\id_rsa");
        }

        [Fact]
        public void Parses_host_with_double_quotes()
        {
            var config = SshConfig.ParseFile("config08");

            config[0].Param.ShouldEqual("Host");
            config[0].Value.ShouldEqual("foo \"!*.bar\"");
        }


        [Fact]
        public void Converts_object_back_to_string()
        {
            var fixture = File.ReadAllText("config");
            var config = SshConfig.ParseFile("config");

            Assert.Contains(config.ToString(), fixture);
        }


        [Fact]
        public void Converts_to_string_with_whitespace_and_comments_in_place()
        {
            var fixture = File.ReadAllText("config09");
            var config = SshConfig.ParseFile("config09");
            config.ToString().ShouldEqual(fixture);
        }


        [Fact]
        public void Converts_IdentityFile_entires_with_double_quotes_to_string()
        {
            var fixture = File.ReadAllText("config10");
            var config = SshConfig.ParseFile("config10");

            config.ToString().ShouldEqual(fixture);
        }


        [Fact]
        public void Gets_result_by_host_with_globbing()
        {
            var config = SshConfig.ParseFile("config");
            var opts = config.Compute("tahoe2");

            opts["User"].ShouldEqual("nil");
            opts.User.ShouldEqual("nil");

            opts.IdentityFile.ShouldEqual("~/.ssh/id_rsa");
//            ((List<object>) opts["IdentityFile"])[0].ShouldEqual("~/.ssh/id_rsa");

            // the first obtained parameter value will be used. So there's no way to
            // override parameter values.
            opts["ServerAliveInterval"].ShouldEqual("80");

            // the computed result is flat on purpose.
            opts = config.Compute("tahoe1");
            opts["Compression"].ShouldEqual("yes");
            opts["ControlMaster"].ShouldEqual("auto");
            opts["ControlPath"].ShouldEqual("~/.ssh/master-%r@%h:%p");
            opts["Host"].ShouldEqual("tahoe1");
            opts.Host.ShouldEqual("tahoe1");
            opts["HostName"].ShouldEqual("tahoe1.com");
            opts.HostName.ShouldEqual("tahoe1.com");
            opts["IdentityFile"].ShouldEqual("~/.ssh/id_rsa");
            opts.IdentityFile.ShouldEqual("~/.ssh/id_rsa");
            opts["ProxyCommand"].ShouldEqual("ssh -q gateway -W %h:%p");
            opts["ServerAliveInterval"].ShouldEqual("80");
            opts["User"].ShouldEqual("nil");
            opts.User.ShouldEqual("nil");
            opts["ForwardAgent"].ShouldEqual("true");
            opts.ForwardAgent.ShouldEqual("true");
        }


        [Fact]
        public void Gets_by_host_with_globbing()
        {
            var config = SshConfig.ParseFile("config02");
            var result = config.Compute("example1");
            result["Host"].ShouldEqual("example1");
            result["HostName"].ShouldEqual("example1.com");
            result["User"].ShouldEqual("simon");
            result["Port"].ShouldEqual("1000");
            result["IdentityFile"].ShouldEqual("/path/to/key");
        }


        [Fact]
        public void Find_returns_null_when_none_found()
        {
            var config = SshConfig.ParseFile("config");
            config.FindNodeByHost("not.exist").ShouldBeNull();
        }


        [Fact]
        public void Finds_by_host()
        {
            var config = SshConfig.ParseFile("config");

            var result = config.FindNodeByHost("tahoe1");
            result.Type.ShouldEqual(NodeType.Directive);
            result.Before.ShouldEqual("");
            result.After.ShouldEqual("\n");
            result.Param.ShouldEqual("Host");
            result.Separator.ShouldEqual(" ");
            result.Value.ShouldEqual("tahoe1");

            var c1 = result.Config[0];
            c1.Type.ShouldEqual(NodeType.Directive);
            c1.Before.ShouldEqual("  ");
            c1.After.ShouldEqual("\n");
            c1.Param.ShouldEqual("HostName");
            c1.Separator.ShouldEqual(" ");
            c1.Value.ShouldEqual("tahoe1.com");

            var c2 = result.Config[1];
            c2.Type.ShouldEqual(NodeType.Directive);
            c2.Before.ShouldEqual("  ");
            c2.After.ShouldEqual("\n\n");
            c2.Param.ShouldEqual("Compression");
            c2.Separator.ShouldEqual(" ");
            c2.Value.ShouldEqual("yes");


            result = config.FindNodeByHost("*");
            result.Type.ShouldEqual(NodeType.Directive);
            result.Before.ShouldEqual("");
            result.After.ShouldEqual("\n");
            result.Param.ShouldEqual("Host");
            result.Separator.ShouldEqual(" ");
            result.Value.ShouldEqual("*");

            c1 = result.Config[0];
            c1.Type.ShouldEqual(NodeType.Directive);
            c1.Before.ShouldEqual("  ");
            c1.After.ShouldEqual("\n\n");
            c1.Param.ShouldEqual("IdentityFile");
            c1.Separator.ShouldEqual(" ");
            c1.Value.ShouldEqual("~/.ssh/id_rsa");
        }


        [Fact]
        public void Removes_by_host()
        {
            var config = SshConfig.ParseFile("config");
            var length = config.Count;

            config.RemoveByHost("no.such.host");
            config.Count.ShouldEqual(length);

            config.RemoveByHost("tahoe2");
            config.FindNodeByHost("tahoe2").ShouldBeNull();
            config.Count.ShouldEqual(length - 1);
        }

        [Fact]
        public void Appends_new_lines()
        {
            var cfg = SshConfig.ParseFile("config02");

            cfg.Add(new Dictionary<string, string>
            {
                {"Host", "example2.com"},
                {"User", "pegg"},
                {"IdentityFile", "~/.ssh/id_rsa"}
            });

            var opts = cfg.Compute("example2.com");

            opts["User"].ShouldEqual("pegg");
            opts["IdentityFile"].ShouldEqual("~/.ssh/id_rsa");


            var result = cfg.FindNodeByHost("example2.com");

            result.Type.ShouldEqual(NodeType.Directive);
            result.Before.ShouldEqual("");
            result.After.ShouldEqual("\n");
            result.Separator.ShouldEqual(" ");
            result.Value.ShouldEqual("example2.com");
            var c1 = result.Config[0];
            var c2 = result.Config[1];

            c1.Type.ShouldEqual(NodeType.Directive);
            c1.Before.ShouldEqual("  ");
            c1.After.ShouldEqual("\n");
            c1.Param.ShouldEqual("User");
            c1.Separator.ShouldEqual(" ");
            c1.Value.ShouldEqual("pegg");

            c2.Type.ShouldEqual(NodeType.Directive);
            c2.Before.ShouldEqual("  ");
            c2.After.ShouldEqual("\n\n");
            c2.Param.ShouldEqual("IdentityFile");
            c2.Separator.ShouldEqual(" ");
            c2.Value.ShouldEqual("~/.ssh/id_rsa");
        }

        [Fact]
        public void Appends_with_original_indentation_recognised()
        {
            var cfg = SshConfig.ParseFile("config03");

            cfg.Add(new Dictionary<string, string>
            {
                {"Host", "example3.com"},
                {"User", "paul"}
            });

            var result = cfg.FindNodeByHost("example3.com");
            result.Type.ShouldEqual(NodeType.Directive);
            result.Before.ShouldEqual("");
            result.After.ShouldEqual("\n");
            result.Param.ShouldEqual("Host");
            result.Separator.ShouldEqual(" ");
            result.Value.ShouldEqual("example3.com");

            var c1 = result.Config[0];
            c1.Type.ShouldEqual(NodeType.Directive);
            c1.Before.ShouldEqual("\t");
            c1.After.ShouldEqual("\n\n");
            c1.Param.ShouldEqual("User");
            c1.Separator.ShouldEqual(" ");
            c1.Value.ShouldEqual("paul");
        }

        [Fact]
        public void Adds_host_with_alias_using_dictionary()
        {
            var d = new Dictionary<string, string>
            {
                { "Host", "test1" },
                { "HostName", "jeremyskinner.co.uk" },
                { "User", "jeremy" },
                { "Port", "123" }
            };

            var cfg = SshConfig.ParseFile("config");
            cfg.Add(d);

            var host = cfg.FindNodeByHost("test1");
            host.ShouldNotBeNull();
            host.Value.ShouldEqual("test1");
            host.Param.ShouldEqual("Host");
            host.Config[0].Param.ShouldEqual("HostName");
            host.Config[0].Value.ShouldEqual("jeremyskinner.co.uk");
            host.Config[1].Param.ShouldEqual("User");
            host.Config[1].Value.ShouldEqual("jeremy");
            host.Config[2].Param.ShouldEqual("Port");
            host.Config[2].Value.ShouldEqual("123");
        }
        
        [Fact]
        public void Adds_host_with_alias_using_hash()
        {
            var d = new Hashtable
            {
                { "Host", "test1" },
                { "HostName", "jeremyskinner.co.uk" },
                { "User", "jeremy" },
                { "Port", "123" }
            };

            var cfg = SshConfig.ParseFile("config");
            cfg.Add(d);

            var host = cfg.FindNodeByHost("test1");
            host.ShouldNotBeNull();
            host.Value.ShouldEqual("test1");
            host.Param.ShouldEqual("Host");
            
            //Can't rely on index - hashtable not ordered

            host.Config.Count.ShouldEqual(3);
            host.Config.AsEnumerable().Single(x => x.Param == "HostName").Value.ShouldEqual("jeremyskinner.co.uk");
            host.Config.AsEnumerable().Single(x => x.Param == "User").Value.ShouldEqual("jeremy");
            host.Config.AsEnumerable().Single(x => x.Param == "Port").Value.ShouldEqual("123");
        }
    }

}