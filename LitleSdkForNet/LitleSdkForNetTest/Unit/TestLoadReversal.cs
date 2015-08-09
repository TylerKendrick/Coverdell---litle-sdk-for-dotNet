﻿using System;
using System.Collections.Generic;
using System.Text;
using Litle.Sdk.Requests;
using NUnit.Framework;
using Litle.Sdk;
using Moq;
using System.Text.RegularExpressions;


namespace Litle.Sdk.Test.Unit
{
    [TestFixture]
    class TestLoadReversal
    {        
        private LitleOnline litle;

        [TestFixtureSetUp]
        public void SetUpLitle()
        {
            litle = new LitleOnline();
        }

        [Test]
        public void TestSimple()
        {
            LoadReversal loadReversal = new LoadReversal();
            loadReversal.id = "a";
            loadReversal.reportGroup = "b";
            loadReversal.LitleTxnId = "123";

            var mock = new Mock<Communications>();

            mock.Setup(Communications => Communications.HttpPost(It.IsRegex(".*<litleTxnId>123</litleTxnId>.*", RegexOptions.Singleline), It.IsAny<Dictionary<String, String>>()))
                .Returns("<litleOnlineResponse version='8.22' response='0' message='Valid Format' xmlns='http://www.litle.com/schema'><loadReversalResponse><litleTxnId>123</litleTxnId></loadReversalResponse></litleOnlineResponse>");

            Communications mockedCommunication = mock.Object;
            litle.SetCommunication(mockedCommunication);
            loadReversalResponse response = litle.LoadReversal(loadReversal);
            Assert.AreEqual("123", response.litleTxnId);
        }


    }
}
