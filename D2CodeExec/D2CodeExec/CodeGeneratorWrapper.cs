using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tech.CodeGeneration;
using System.Text.RegularExpressions;
using D2;

namespace D2CodeExec
{
    public interface ICodeGeneratorWrapper
    {
        /// <summary>
        /// Checks code for syntax errors
        /// </summary>
        /// <param name="code">code to check</param>
        /// <returns>exception string if there are any errors in code, "ОК" otherwise</returns>
        string CheckCode(string code);
        /// <summary>
        /// Starts script execution in separate thread
        /// </summary>
        /// <param name="code">Script to execute</param>
        /// <returns>Thread ID</returns>
        int ExecuteCode(string code);
        /// <summary>
        /// Adds namespace to list of namespaces used for code execution
        /// </summary>
        /// <param name="namespaceName">Name of the used namespace</param>
        /// <param name="namespaceRef">Reference to DLL which contains used namespace</param>
        void UseNamespace(string namespaceName, string namespaceRef);
        /// <summary>
        /// Clears all used namespaces
        /// </summary>
        void ClearUsedNamespaces();
        /// <summary>
        /// Cancels task execution
        /// </summary>
        /// <param name="taskId">task Id returned by ExecuteCode</param>
        void CancelCodeExecutionTask(int taskId);
        /// <summary>
        /// Returns IDs of all tasks curently running
        /// </summary>
        /// <returns>array of tasks IDs</returns>
        int[] GetTasksIDs();
    }

    internal class D2Task
    {
        public D2Task()
        {
            TaskCancellationTokenSource = new CancellationTokenSource();
        }
        public Task<string> Task { get; set; }
      
        public CancellationTokenSource TaskCancellationTokenSource { get; }        
    }
    public class CodeGeneratorWrapper : ICodeGeneratorWrapper
    {
        private object _d2ShedulerInterfaceLock;
        private object _logFileLock;        
        private static object _codeGeneratorLock;
        private static CodeExecutionParameters _codeExecutionParameters;
                
        //private List<Task<string>> _executingTasks;
        //private List<CancellationTokenSource> _cancellationTokenSources;
        private List<D2Task> _currentlyExecutingTasks;
        //=======

        private D2ShedulerInterfaceClass shedulerInterfaceClass;
        private ID2ShedulerInterface _d2ShedulerInterface;

        //Важно чтобы коснтруктор был public - иначе:
        //warning MSB3214: "D:\Work\MFC\PROJECTS\Delta2\D2_VS2015\D2CodeExec\D2CodeExec\bin\Release\D2CodeExec.dll" не содержит типы, которые могут быть зарегистрированы для взаимодействия объектов COM.
        public CodeGeneratorWrapper()
        {
            _codeExecutionParameters = new CodeExecutionParameters();
            //_executingTasks = new List<Task<string>>();
            //_cancellationTokenSources = new List<CancellationTokenSource>();
            _currentlyExecutingTasks = new List<D2Task>();
            _d2ShedulerInterfaceLock = new object();
            _logFileLock = new object();
            _codeGeneratorLock = new object();
        }

        public string CheckCode(string code)
        {            
            string result = "OK";
            try
            {
                CodeGenerator.CreateCode<int>(code,
                                                _codeExecutionParameters.UsedNamespacesNames,
                                                _codeExecutionParameters.UsedNamespacesRefs,
                                                new CodeParameter("__codeGeneratorWrapper_CancellationToken", typeof(CancellationToken)));

                //CodeGenerator.CreateCode<int>(code, _codeExecutionParameters.UsedNamespacesNames, _codeExecutionParameters.UsedNamespacesRefs);                
            }
            catch (Exception exception)
            {
                result = exception + "\n" + exception.StackTrace;
            }            
            return result;
        }

        public int ExecuteCode(string code)
        {                        
            D2Task newTask = new D2Task();
            _codeExecutionParameters.CurrentCodeToExecute = PrepareCodeForCancelationAbility(code);
            _codeExecutionParameters.Cancellation = newTask.TaskCancellationTokenSource.Token;
            newTask.Task = Task.Factory.StartNew<string>(() => CodeExecution(_codeExecutionParameters), _codeExecutionParameters.Cancellation);
            newTask.Task.ContinueWith(ReportCodeExecution);           
            _currentlyExecutingTasks.Add(newTask);                     
            return newTask.Task.Id;          
        }

        public void CancelCodeExecutionTask(int taskId)
        {
            _currentlyExecutingTasks.First( t => t.Task.Id == taskId)?.TaskCancellationTokenSource.Cancel();            
        }

        public void UseNamespace(string namespaceName, string namespaceRef)
        {
            _codeExecutionParameters.AddNamespace(namespaceName, namespaceRef);
        }

        public void ClearUsedNamespaces()
        {            
            _codeExecutionParameters.ClearAllNamespaces();
        }

        public int[] GetTasksIDs()
        {
            return (from task in _currentlyExecutingTasks select task.Task.Id).ToArray();
        }
        /// <summary>
        /// Inserts (!__codeGeneratorWrapper_CancellationToken.IsCancellationRequested)&& in do-while-for conditions in code
        /// </summary>
        /// <param name="code">code to modify</param>
        public string PrepareCodeForCancelationAbility(string code)
        {            
            string processedCode = " "+code;//inserting whitespace if for-while is the first keyword in code                        
            string insertedConditionString = "(!__codeGeneratorWrapper_CancellationToken.IsCancellationRequested)";
            string forKeywordPattern = @"[\s;]for[\s.]*\([^;]*;";            
            string whileKeywordPattern = @"[\s;]while[\s.]*\(";

            int insertedSymbolsCount = 0;
            foreach (Match match in Regex.Matches(processedCode, forKeywordPattern))
            {
                string andString = "&&";
                if (processedCode[match.Index + match.Length] == ';')
                    andString = "";
                processedCode = processedCode.Insert(match.Index + match.Length + insertedSymbolsCount, insertedConditionString + andString);
                insertedSymbolsCount += insertedConditionString.Length + andString.Length;
            }
           
            insertedSymbolsCount = 0;
            foreach (Match match in Regex.Matches(processedCode, whileKeywordPattern))
            {
                string andString = "&&";
                processedCode = processedCode.Insert(match.Index + match.Length + insertedSymbolsCount, insertedConditionString + andString);
                insertedSymbolsCount += insertedConditionString.Length + andString.Length;
            }            
            return processedCode;
        }
        /// <summary>
        /// Execution task thread
        /// </summary>
        /// <param name="codeExecutionParameters">parameters for thread</param>
        /// <returns></returns>
        internal static string CodeExecution(CodeExecutionParameters codeExecutionParameters)
        {            
            string result = "OK";
            try
            {
                IGeneratedCode<int> executedCode = null;
                lock (_codeGeneratorLock)
                {
                    executedCode = CodeGenerator.CreateCode<int>(codeExecutionParameters.CurrentCodeToExecute,
                        codeExecutionParameters.UsedNamespacesNames,
                        codeExecutionParameters.UsedNamespacesRefs,
                        new CodeParameter("__codeGeneratorWrapper_CancellationToken", typeof (CancellationToken)));
                }
                executedCode.Execute(_codeExecutionParameters.Cancellation);
            }
            catch (Exception exception)
            {
                result = exception + exception.StackTrace;                                
            }           
            return result;
        }

        internal bool IsD2ShedulerInterfaceValid()
        {
            if (_d2ShedulerInterface == null)
            {
                if (shedulerInterfaceClass == null)
                    shedulerInterfaceClass = new D2ShedulerInterfaceClass();
            }            
            _d2ShedulerInterface = (ID2ShedulerInterface)shedulerInterfaceClass;
            if (_d2ShedulerInterface != null)
                return true;
            return false;
        }


        /// <summary>
        /// Reports status string from CodeExecution function to D2Sheduler
        /// </summary>
        /// <param name="task">ID of executed task</param>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptionsAttribute()]
        internal void ReportCodeExecution(Task<string> task)
        {
            //Log($"task {task.Id} reported with status {task.Status}");
            lock (_d2ShedulerInterfaceLock)
            {
                if (IsD2ShedulerInterfaceValid())
                {
                    //Log($"caling _d2ShedulerInterface.ReportTaskEnd for task {task.Id} with status {task.Status}...");
                    try
                    {
                        _d2ShedulerInterface.ReportTaskEnd(task.Result, task.Id);
                    }
                    catch(AggregateException ex)
                    {                        
                        //Log($"SECOND caling _d2ShedulerInterface.ReportTaskEnd for task {task.Id} with status {task.Status}...");
                        _d2ShedulerInterface.ReportTaskEnd("OK", task.Id);
                        //Log($"SECOND caling _d2ShedulerInterface.ReportTaskEnd for task {task.Id} with status {task.Status} SUCCESS");
                    }                                                                      
                }                    
            }
            
            int removed = _currentlyExecutingTasks.RemoveAll(t => t.Task.Id == task.Id);
            //Log($"_currentlyExecutingTasks for task {task.Id} with status {task.Status} removed {removed}");
            //Debug.WriteLine("Task {0} ends with Result: {1} tasks removed: {2}", task.Id, task.Result, removed);
        }

        /*void Log(string logstring)
        {
            lock (_logFileLock)
            {
                logstring += "\r\n";
                File.AppendAllText(@"C:\ReportCodeExecution.txt", logstring);
            }              
        }*/
    }    
}
