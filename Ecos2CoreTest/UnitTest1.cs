/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using Ecos2Core;
using Ecos2Core.Replies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ecos2CoreTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var cmd0 = CommandFactory.Create("request(1002, view)");
            cmd0.Name.ShouldBeEquivalentTo("request");
            cmd0.Arguments.Count.Should().Be(2);

            var cmd1 = CommandFactory.Create("get(100)");
            cmd1.Name.ShouldBeEquivalentTo("get");
            cmd1.Arguments.Count.Should().Be(1);

            var cmd2 = CommandFactory.Create("release( 11, view, viewswitch)");
            cmd2.Name.ShouldBeEquivalentTo("release");
            cmd2.Arguments.Count.Should().Be(3);

            var cmd3 = CommandFactory.Create("set( 1, stop)");
            cmd3.Name.ShouldBeEquivalentTo("set");
            cmd3.Arguments.Count.Should().Be(2);

            var cmd4 = CommandFactory.Create("get( 26, state)");
            cmd4.Name.ShouldBeEquivalentTo("get");
            cmd4.Arguments.Count.Should().Be(2);

            var cmd5 = CommandFactory.Create("release(10, view)");
            cmd5.Name.ShouldBeEquivalentTo("release");
            cmd5.Arguments.Count.Should().Be(2);

            var cmd6 = CommandFactory.Create("create(10,  addr[1000], name[\"Ae3/6II SBB\"],   protocol[DCC14], append)");
            cmd6.Name.ShouldBeEquivalentTo("create");
            cmd6.Arguments.Count.Should().Be(5);
            cmd6.ToString().ShouldBeEquivalentTo("create(10, addr[1000], name[\"Ae3/6II SBB\"], protocol[DCC14], append)");

            var cmd7 = CommandFactory.Create("ries()");
            cmd7.Name.ShouldBeEquivalentTo("Unknown");
            cmd7.Arguments.Count.Should().Be(0);

            var cmd8 = CommandFactory.Create("test()");
            cmd8.Name.ShouldBeEquivalentTo("test");
            cmd8.Arguments.Count.Should().Be(0);

            var cmd9 = CommandFactory.Create("test(\"Hello world!\")");
            cmd9.Name.ShouldBeEquivalentTo("test");
            cmd9.Arguments.Count.Should().Be(1);
        }

        [TestMethod]
        public void CheckArgument0()
        {
            CommandArgument arg0 = new CommandArgument {Name = "addr"};
            arg0.Parameter.Add("1000");
            arg0.ToString().Should().BeEquivalentTo("addr[1000]");

            CommandArgument arg1 = new CommandArgument { Name = "name" };
            arg1.Parameter.Add("Ae3/6II SBB");
            arg1.ToString().Should().BeEquivalentTo("name[\"Ae3/6II SBB\"]");

            CommandArgument arg2 = new CommandArgument {Name = "addr"};
            arg2.Parameter.Add("1000");
            arg2.Parameter.Add("1010");
            arg2.Parameter.Add("1020");
            arg2.ToString().Should().BeEquivalentTo("addr[1000,1010,1020]");
        }

        [TestMethod]
        public void TestListEntries()
        {
            var e0 = new ListEntry();
            e0.Parse("1002 name[\"kleine Schwarze\"] addr[2] protocol[MM14]\r\n");
            e0.ObjectId.Should().Be(1002);
            e0.Arguments.Count.Should().Be(3);

            var e1 = new ListEntry();
            e1.Parse("1001 name[\"Kompressor\"] addr[78] protocol[MM14]\r\n");
            e1.ObjectId.Should().Be(1001);
            e1.Arguments.Count.Should().Be(3);

            var e2 = new ListEntry();
            e2.Parse("20001 addr[8] protocol[DCC]");
            e2.ObjectId.Should().Be(20001);
            e2.Arguments.Count.Should().Be(2);

            var e3 = new ListEntry();
            e3.Parse("101 view[objectclass,view,listview,control,ports,state,railcom]");
            e3.ObjectId.Should().Be(101);
            e3.Arguments.Count.Should().Be(1);
            e3.Arguments[0].Parameter.Count.Should().Be(7);
            e3.Arguments[0].Parameter.Contains("listview").Should().BeTrue();
        }

        [TestMethod]
        public void TestReplies0()
        {
            string reply = "<REPLY queryObjects(10, addr, name, protocol)>\r\n";
            reply += "1002 name[\"kleine Schwarze\"] addr[2] protocol[MM14]\r\n";
            reply += "1005 name[\"Dampf #2\"] addr[20] protocol[MM14]\r\n";
            reply += "1007 name[\"Kompressorlok\"] addr[78] protocol[MM14]\r\n";
            reply += "1004 name[\"Schwarz Diesel\"] addr[2] protocol[MM14]\r\n";
            reply += "1006 name[\"DIESEL rot\"] addr[21] protocol[MM14]\r\n";
            reply += "1008 name[\"BR10\"] addr[10] protocol[MM14]\r\n";
            reply += "1000 name[\"Ae3 /6II SBB\"] addr[1000] protocol[DCC28]\r\n";
            reply += "1009 name[\"DAMPF2\"] addr[20] protocol[MM14]\r\n";
            reply += "1001 name[\"Kompressor\"] addr[78] protocol[MM14]\r\n";
            reply += "1003 name[\"kleine Schwarze\"] addr[2] protocol[MM14]\r\n";
            reply += "<END 0 (OK)>\r\n";

            var msg = new ReplyBlock();
            bool r = msg.Parse(reply);
            r.Should().BeTrue();
            msg.ListEntries.Count.ShouldBeEquivalentTo(10);
            msg.Result.ErrorCode.Should().Be(0);
            msg.Result.ErrorMessage.ShouldBeEquivalentTo("OK");
            msg.Command.Name.ShouldBeEquivalentTo("queryObjects");
        }

        [TestMethod]
        public void TestReplies1()
        {
            string reply = "<REPLY get(100)>\r\n";
            reply += "100 objectclass[feedback - module]\r\n";
            reply += "100 view[objectclass, view, listview, control, ports, state, railcom]\r\n";
            reply += "100 listview[none]\r\n";
            reply += "100 control[none]\r\n";
            reply += "100 ports[16]\r\n";
            reply += "100 state[0x0]\r\n";
            reply += "100 railcom[...]\r\n";
            reply += "<END 0 (OK)>\r\n";

            var msg = new ReplyBlock();
            bool r = msg.Parse(reply);
            r.Should().BeTrue();
            msg.ListEntries.Count.ShouldBeEquivalentTo(7);
            msg.Result.ErrorCode.Should().Be(0);
            msg.Result.ErrorMessage.ShouldBeEquivalentTo("OK");
            msg.Command.Name.ShouldBeEquivalentTo("get");
        }

        [TestMethod]
        public void TestReplies2()
        {
            string reply = "<REPLY get(26, state)>\r\n";
            reply  += "<END 11 (unknown option at 9)>\r\n";

            var msg = new ReplyBlock();
            bool r = msg.Parse(reply);
            r.Should().BeTrue();
            msg.ListEntries.Count.ShouldBeEquivalentTo(0);
            msg.Result.ErrorCode.Should().Be(11);
            msg.Result.ErrorMessage.ShouldBeEquivalentTo("unknown option at 9");
            msg.Command.Name.ShouldBeEquivalentTo("get");
        }
    }
}
