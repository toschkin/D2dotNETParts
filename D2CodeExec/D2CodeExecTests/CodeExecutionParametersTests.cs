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
    public class CodeExecutionParametersTests
    {
        [TestMethod()]
        public void CodeExecutionParametersTest()
        {
            CodeExecutionParameters parameters = new CodeExecutionParameters();

            Assert.AreEqual(parameters.DefaultNamespacesCollection.Count, parameters.UsedNamespacesNames.Count);
            Assert.AreEqual(parameters.DefaultNamespaceRefsCollection.Count, parameters.UsedNamespacesRefs.Count);
            Assert.AreEqual("", parameters.CurrentCodeToExecute);
            for (int i = 0; i < parameters.UsedNamespacesNames.Count; i++)
            {
                Assert.AreEqual(parameters.DefaultNamespacesCollection[i], parameters.UsedNamespacesNames[i]);
            }
            for (int i = 0; i < parameters.UsedNamespacesRefs.Count; i++)
            {
                Assert.AreEqual(parameters.DefaultNamespaceRefsCollection[i], parameters.UsedNamespacesRefs[i]);
            }            
            Assert.AreEqual(CancellationToken.None, parameters.Cancellation);            
        }

        [TestMethod()]
        public void CopyTest()
        {
            CodeExecutionParameters parameters1 = new CodeExecutionParameters();
            CodeExecutionParameters parameters2 = new CodeExecutionParameters();
            parameters1.CurrentCodeToExecute = "MessageBox.Show(\"Test!\");";
            int count1 = parameters1.UsedNamespacesNames.Count;
            parameters1.AddNamespace("System.Windows.Forms", "System.Windows.Forms.dll");
            parameters2.Copy(parameters1);
            parameters1.ClearAllNamespaces();                       
            Assert.AreEqual(count1 + 1, parameters2.UsedNamespacesNames.Count);
            Assert.AreEqual(count1 + 1, parameters2.UsedNamespacesRefs.Count);
            Assert.AreEqual("System.Windows.Forms", parameters2.UsedNamespacesNames[count1]);
            Assert.AreEqual("System.Windows.Forms.dll", parameters2.UsedNamespacesRefs[count1]);
            Assert.AreEqual("MessageBox.Show(\"Test!\");", parameters2.CurrentCodeToExecute);
        }

        [TestMethod()]
        public void AddNamespaceTest()
        {
            CodeExecutionParameters parameters1 = new CodeExecutionParameters();
            int count1 = parameters1.UsedNamespacesNames.Count;
            parameters1.AddNamespace("System.Windows.Forms", "System.Windows.Forms.dll");
            parameters1.AddNamespace("System.Windows.Forms", "System.Windows.Forms.dll");
            Assert.AreEqual(count1 + 1, parameters1.UsedNamespacesNames.Count);
            Assert.AreEqual(count1 + 1, parameters1.UsedNamespacesRefs.Count);
            Assert.AreEqual("System.Windows.Forms", parameters1.UsedNamespacesNames[count1]);
            Assert.AreEqual("System.Windows.Forms.dll", parameters1.UsedNamespacesRefs[count1]);           
        }

        [TestMethod()]
        public void ClearAllNamespacesTest()
        {
            CodeExecutionParameters parameters1 = new CodeExecutionParameters();
            parameters1.ClearAllNamespaces();
            Assert.AreEqual(parameters1.DefaultNamespacesCollection.Count, parameters1.UsedNamespacesNames.Count);
            Assert.AreEqual(parameters1.DefaultNamespaceRefsCollection.Count, parameters1.UsedNamespacesRefs.Count);            
        }
    }
}