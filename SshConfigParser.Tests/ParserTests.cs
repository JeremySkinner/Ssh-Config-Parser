using System;
using System.IO;
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
  public void Gets_result_by_host()
  {
    var config = SshConfig.ParseFile("config");
    var opts = config.compute("tahoe2");

    opts.User.ShouldEqual("nil");
    opts.IdentityFile.ShouldEqual(['~/.ssh/id_rsa'])

    // the first obtained parameter value will be used. So there's no way to
    // override parameter values.
    opts.ServerAliveInterval).to.eql(80)

    // the computed result is flat on purpose.
    config.compute("tahoe1")).to.eql({
      Compression: 'yes',
      ControlMaster: 'auto',
      ControlPath: '~/.ssh/master-%r@%h:%p',
      Host: 'tahoe1',
      HostName: 'tahoe1.com',
      IdentityFile: [
        '~/.ssh/id_rsa'
      ],
      ProxyCommand: 'ssh -q gateway -W %h:%p',
      ServerAliveInterval: '80',
      User: 'nil',
      ForwardAgent: 'true'
    })
  }


    [Fact]
    public void Gets_by_host_with_globbing()
    {
      var config2 = SshConfig.ParseFile("config02");

    config2.compute("example1")).to.eql({
      Host: 'example1',
      HostName: 'example1.com',
      User: 'simon',
      Port: '1000',
      IdentityFile: [
        '/path/to/key'
      ]
    })
  }


  [Fact] public void Find_with_nothing_generates_error()
  {
    var config = SshConfig.ParseFile("config");
    function() { config.find() }).to.throwException()
    function() { config.find({}) }).to.throwException()
  }


  [Fact] public void Find_returns_null_when_none_found()
  {
    var config = SshConfig.ParseFile("config");
    config.find({ Host: 'not.exist' })).to.be(null)
  }


  [Fact] public void Finds_by_host()
  {
    var config = SshConfig.ParseFile("config");

    config.find({ Host: 'tahoe1' })).to.eql({
      type: DIRECTIVE,
      before: '',
      after: '\n',
      param: 'Host',
      separator: ' ',
      value: 'tahoe1',
      config: [{
        type: DIRECTIVE,
        before: '  ',
        after: '\n',
        param: 'HostName',
        separator: ' ',
        value: 'tahoe1.com'
      }, {
        type: DIRECTIVE,
        before: '  ',
        after: '\n\n',
        param: 'Compression',
        separator: ' ',
        value: 'yes'
      }]
    })

    config.find({ Host: '*' })).to.eql({
      type: DIRECTIVE,
      before: '',
      after: '\n',
      param: 'Host',
      separator: ' ',
      value: '*',
      config: [{
        type: DIRECTIVE,
        before: '  ',
        after: '\n\n',
        param: 'IdentityFile',
        separator: ' ',
        value: '~/.ssh/id_rsa'
      }]
    })
  }


  [Fact] public void Removes_by_host()
  {
    var config = SshConfig.ParseFile("config");
    var length = config.length

    config.remove({ Host: 'no.such.host' })
    config.length.ShouldEqual(length)

    config.remove({ Host: 'tahoe2' })
    config.find({ Host: 'tahoe2' })).to.be(null)
    config.length.ShouldEqual(length - 1)

    function() { config.remove() }).to.throwException()
    function() { config.remove({}) }).to.throwException()
  }

  [Fact] public void Appends_new_lines() {
    const config = SshConfig.ParseFile("config02"))

    cfg.append({
      Host: 'example2.com',
      User: 'pegg',
      IdentityFile: '~/.ssh/id_rsa'
    })

    const opts = cfg.compute("example2.com")
    opts.User).to.eql("pegg")
    opts.IdentityFile).to.eql(['~/.ssh/id_rsa'])
    cfg.find({ Host: 'example2.com' })).to.eql({
      type: DIRECTIVE,
      before: '',
      after: '\n',
      param: 'Host',
      separator: ' ',
      value: 'example2.com',
      config: [{
        type: DIRECTIVE,
        before: '  ',
        after: '\n',
        param: 'User',
        separator: ' ',
        value: 'pegg'
      },{
        type: DIRECTIVE,
        before: '  ',
        after: '\n\n',
        param: 'IdentityFile',
        separator: ' ',
        value: '~/.ssh/id_rsa'
      }]
    })
  })

  [Fact] public void Appends_with_original_indentation_recognised() {
    const config = SshConfig.ParseFile("config03");

    cfg.append({
      Host: 'example3.com',
      User: 'paul'
    })

    cfg.find({ Host: 'example3.com' })).to.eql({
      type: DIRECTIVE,
      before: '',
      after: '\n',
      param: 'Host',
      separator: ' ',
      value: 'example3.com',
      config: [{
        type: DIRECTIVE,
        before: '\t',
        after: '\n\n',
        param: 'User',
        separator: ' ',
        value: 'paul'
      }]
    })
  }
    }
}