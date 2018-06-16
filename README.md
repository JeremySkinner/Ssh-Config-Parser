# SSH Config File Parser for .NET

This is a parser for the OpenSSH config file format written in .NET. It is a port of https://github.com/dotnil/ssh-config.

# Usage

```
# Assuming the following config file
Host server1
  HostName server1.jeremyskinner.co.uk
  IdentityFile ~/.ssh/id_rsa
```

```csharp


var config = SshConfig.ParseConfig("path/to/ssh/config");
// Find a host
var host = config.Find("server1");
Console.WriteLine(host.Host); // server1
Console.WriteLine(host.HostName); // server1.jeremyskinner.co.uk
Console.WriteLine(host.IdentityFile); // ~/.ssh/id_rsa

// Also accessible via indexer along with any other properties
Console.WriteLine(host["Host"]);
Console.WriteLine(host["HostName"]);

// Add a new host
config.Add(new SshHost 
{
  Host = "server2.jeremyskinner.co.uk",
  IdentityFile = "~/.ssh/id_rsa"
});

// Convert the config back to a string which can be written to the .ssh/config file
string rawOutput = config.ToString();
```