using Microsoft.VisualStudio.TestTools.UnitTesting;
using D2CodeExec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace D2CodeExec.Tests
{
    [TestClass()]
    public class CodeGeneratorWrapperTests
    {
        [TestMethod()]
        public void CodeGeneratorWrapperTest()
        {
            CodeGeneratorWrapper testCodeGeneratorWrapper = new CodeGeneratorWrapper();
        }

        [TestMethod()]
        public void CheckCodeTest()
        {
            CodeGeneratorWrapper testCodeGeneratorWrapper = new CodeGeneratorWrapper();
            testCodeGeneratorWrapper.UseNamespace("System.Windows.Forms", "System.Windows.Forms.dll");
            Assert.AreEqual("OK", testCodeGeneratorWrapper.CheckCode("MessageBox.Show(\"Test!\");"));
            Assert.AreNotEqual("OK", testCodeGeneratorWrapper.CheckCode("MessageBox.Show"));
        }

        [TestMethod()]
        public void UseNamespaceTest()
        {
            CodeGeneratorWrapper testCodeGeneratorWrapper = new CodeGeneratorWrapper();
            Assert.AreNotEqual("OK", testCodeGeneratorWrapper.CheckCode("MessageBox.Show(\"Test!\");"));
            testCodeGeneratorWrapper.UseNamespace("System.Windows.Forms", "System.Windows.Forms.dll");
            Assert.AreEqual("OK", testCodeGeneratorWrapper.CheckCode("MessageBox.Show(\"Test!\");"));
        }

        [TestMethod()]
        public void PrepareCodeForCancelationAbilityEmptyForTest()
        {
            CodeGeneratorWrapper testCodeGeneratorWrapper = new CodeGeneratorWrapper();
            string codeSource = "for(;;){Thread.Sleep(500);}";
            string codeExpected = " for(;(!__codeGeneratorWrapper_CancellationToken.IsCancellationRequested);){Thread.Sleep(500);}";
            string codeModified = testCodeGeneratorWrapper.PrepareCodeForCancelationAbility(codeSource);
            Assert.AreEqual(codeExpected, codeModified);
            Assert.AreEqual("OK", testCodeGeneratorWrapper.CheckCode(codeModified));
        }

        [TestMethod()]
        public void PrepareCodeForCancelationAbilityForTest()
        {
            CodeGeneratorWrapper testCodeGeneratorWrapper = new CodeGeneratorWrapper();
            string codeSource = "for\t (int i=0;i<1000;i++){Thread.Sleep(500);}\t;for\r\n(int i=0;i<1000;i++){Thread.Sleep(500);}";
            string codeExpected = " for\t (int i=0;(!__codeGeneratorWrapper_CancellationToken.IsCancellationRequested)&&i<1000;i++){Thread.Sleep(500);}\t;for\r\n(int i=0;(!__codeGeneratorWrapper_CancellationToken.IsCancellationRequested)&&i<1000;i++){Thread.Sleep(500);}";

            string codeModified = testCodeGeneratorWrapper.PrepareCodeForCancelationAbility(codeSource);
            Assert.AreEqual(codeExpected, codeModified);
            Assert.AreEqual("OK", testCodeGeneratorWrapper.CheckCode(codeModified));
        }

        [TestMethod()]
        public void PrepareCodeForCancelationAbilityWhileTest()
        {
            CodeGeneratorWrapper testCodeGeneratorWrapper = new CodeGeneratorWrapper();
            string codeSource = "int x = 0;while\t \t(x<100)\r\n{x++;Thread.Sleep(500);}\twhile\r\n(x<100)\r\n{x++;Thread.Sleep(500);}";
            string codeExpected = " int x = 0;while\t \t((!__codeGeneratorWrapper_CancellationToken.IsCancellationRequested)&&x<100)\r\n{x++;Thread.Sleep(500);}\twhile\r\n((!__codeGeneratorWrapper_CancellationToken.IsCancellationRequested)&&x<100)\r\n{x++;Thread.Sleep(500);}";
            string codeModified = testCodeGeneratorWrapper.PrepareCodeForCancelationAbility(codeSource);
            Assert.AreEqual(codeExpected, codeModified);
            Assert.AreEqual("OK", testCodeGeneratorWrapper.CheckCode(codeModified));
        }

        [TestMethod()]
        public void GetTasksIDsTest()
        {
            CodeGeneratorWrapper testCodeGeneratorWrapper = new CodeGeneratorWrapper();
            string codeSource = "for(int i=0;i<10;i++){Thread.Sleep(1000);}";
            testCodeGeneratorWrapper.UseNamespace("System.Threading", "System.Threading.dll");
            testCodeGeneratorWrapper.ExecuteCode(codeSource);
            testCodeGeneratorWrapper.ExecuteCode(codeSource);
            testCodeGeneratorWrapper.ExecuteCode(codeSource);
            Assert.AreEqual(3, testCodeGeneratorWrapper.GetTasksIDs().Length);
        }

        [TestMethod()]
        public void ExecuteCodeTest()
        {
            CodeGeneratorWrapper testCodeGeneratorWrapper = new CodeGeneratorWrapper();
            for (int i = 0; i < 25; i++)
            {
                string codeSource = "for(int i=0;i<10;i++){Thread.Sleep(100);}";
                testCodeGeneratorWrapper.ExecuteCode(codeSource);
                testCodeGeneratorWrapper.ExecuteCode(codeSource);
                codeSource = "for(int i=0;i<100000;i++){Thread.Sleep(1000);}";
                int id = testCodeGeneratorWrapper.ExecuteCode(codeSource);
                testCodeGeneratorWrapper.CancelCodeExecutionTask(id);
                Thread.Sleep(5000);
            }
            Thread.Sleep(5000);
            Assert.AreEqual(0, testCodeGeneratorWrapper.GetTasksIDs().Length);
        }
    }
}