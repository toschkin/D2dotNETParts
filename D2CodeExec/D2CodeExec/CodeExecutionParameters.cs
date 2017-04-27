using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace D2CodeExec
{
    public class CodeExecutionParameters
    {
        public ReadOnlyCollection<string> DefaultNamespacesCollection = new ReadOnlyCollection<string>(new List<string> { "System", "System.Threading" });
        public ReadOnlyCollection<string> DefaultNamespaceRefsCollection = new ReadOnlyCollection<string>(new List<string> { "System.dll", "System.Threading.dll" });
        public List<string> UsedNamespacesNames { get; private set; }

        public List<string> UsedNamespacesRefs { get; private set; }

        public string CurrentCodeToExecute { get; set; }

        public CancellationToken Cancellation;

        public CodeExecutionParameters()
        {
            UsedNamespacesNames = DefaultNamespacesCollection.ToList();

            UsedNamespacesRefs = DefaultNamespaceRefsCollection.ToList();

            CurrentCodeToExecute = "";

            Cancellation = CancellationToken.None;
        }

        public void Copy(CodeExecutionParameters parameters)
        {
            UsedNamespacesNames = parameters.UsedNamespacesNames.Where(t => t != null).ToList();
            UsedNamespacesRefs = parameters.UsedNamespacesRefs.Where(t => t != null).ToList();
            CurrentCodeToExecute = parameters.CurrentCodeToExecute;
        }

        public void AddNamespace(string namespaceName, string namespaceRef)
        {
            if (namespaceName != null && namespaceRef != null)
            {
                if (UsedNamespacesNames.Any(nsName => nsName.Equals(namespaceName)))
                {
                    return;
                }
                UsedNamespacesNames.Add(namespaceName);
                UsedNamespacesRefs.Add(namespaceRef);
            }
        }

        public void ClearAllNamespaces()
        {
            UsedNamespacesNames.Clear();
            UsedNamespacesRefs.Clear();

            UsedNamespacesNames = DefaultNamespacesCollection.ToList();

            UsedNamespacesRefs = DefaultNamespaceRefsCollection.ToList();
        }
    }
}