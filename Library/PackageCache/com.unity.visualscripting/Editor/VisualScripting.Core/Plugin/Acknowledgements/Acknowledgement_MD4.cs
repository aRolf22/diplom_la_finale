namespace Unity.VisualScripting
{
    [Plugin(BoltCore.ID)]
    internal class Acknowledgement_MD4 : PluginAcknowledgement
    {
        public Acknowledgement_MD4(Plugin plugin) : base(plugin) { }

        public override string title => "MD4 Managed Implementation";
        public override string author => "Motus Technologies Inc., Novell, Inc";
        public override int? copyrightYear => 2005;
        public override string url => "https://github.com/mono/mono/blob/master/mcs/class/Mono.Security/Mono.Security.Cryptography/MD4Managed.cs";

        public override string licenseText =>
@"Author:
Sebastien Pouliot (sebastien@ximian.com)

(C) 2003 Motus Technologies Inc. (http://www.motus.com)
Copyright (C) 2004-2005,2010 Novell, Inc (http://www.novell.com)

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
    }
}
