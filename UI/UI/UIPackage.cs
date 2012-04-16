﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sando.Core;
using Sando.Core.Extensions;
using Sando.Core.Tools;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Indexer.Searching;
using Sando.Parser;
using Sando.SearchEngine;
using Sando.Translation;
using Sando.UI.Monitoring;
using Sando.UI.View;

namespace Sando.UI
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(SearchToolWindow))]
    [Guid(GuidList.guidUIPkgString)]
	// This attribute starts up our extension early so that it can listen to solution events
	[ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
	public sealed class UIPackage: Package
    {    	
    	
		private SolutionMonitor _currentMonitor;
		
		//For classloading... //TODO- eliminate the need for these
    	private List<ProgramElement> _list = new List<ProgramElement>();
		private List<CodeSearchResult> _stuff = new List<CodeSearchResult>();
		private string _other = Configuration.Configuration.GetValue("stuff");
    	private TranslationCode _mycode = TranslationCode.Exception_General_IOException;



    	private SolutionEvents _solutionEvents;

    	private static UIPackage MyPackage
		{
			get;
			set;
		}

    	/// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public UIPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
    		MyPackage = this;
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(SearchToolWindow), 0, true);
			
			
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
			
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
			// Dock Sando to the bottom of Visual Studio.
			windowFrame.SetFramePos(VSSETFRAMEPOS.SFP_fDockBottom, Guid.Empty, 0, 0, 0, 0);

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

		public void EnsureSandoRunning()
		{
			ShowToolWindow(null,null);
		}

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the tool window
                var toolwndCommandID = new CommandID(GuidList.guidUICmdSet, (int)PkgCmdIDList.sandoSearch);
                var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand( menuToolWin );
            }

			var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
			if(dte != null)
			{
				_solutionEvents = dte.Events.SolutionEvents;                
				_solutionEvents.Opened += SolutionHasBeenOpened;
				_solutionEvents.AfterClosing += SolutionHasBeenClosed;
			}

			ExtensionPointsRepository extensionPointsRepository = ExtensionPointsRepository.Instance;
			extensionPointsRepository.RegisterParserImplementation(new List<string>() { ".cs" }, new SrcMLCSharpParser());
			extensionPointsRepository.RegisterParserImplementation(new List<string>() { ".h", ".cpp", ".cxx" }, new SrcMLCppParser());

			extensionPointsRepository.RegisterWordSplitterImplementation(new WordSplitter());

			extensionPointsRepository.RegisterResultsReordererImplementation(new SortByScoreResultsReorderer());

			extensionPointsRepository.RegisterQueryWeightsSupplierImplementation(new QueryWeightsSupplier());
        }

		private void SolutionHasBeenClosed()
		{
		
			if(_currentMonitor != null)
			{
				_currentMonitor.RemoveUpdateListener(SearchViewControl.GetInstance());
				_currentMonitor.Dispose();
				_currentMonitor = null;
			}
		}

		private void SolutionHasBeenOpened()
		{
			_currentMonitor = SolutionMonitorFactory.CreateMonitor();
            _currentMonitor.StartMonitoring();		    
			_currentMonitor.AddUpdateListener(SearchViewControl.GetInstance());
		}

  

		public static UIPackage GetInstance()
		{
			return MyPackage;
		}



    	#endregion

    	public string GetCurrentDirectory()
    	{
			if(_currentMonitor != null)
				return _currentMonitor.GetCurrentDirectory();
			else
				return null;
    	}


		public SolutionKey GetCurrentSolutionKey()
		{
			if(_currentMonitor != null)
				return _currentMonitor.GetSolutionKey();
			else
				return null;
		}

    	#region Implementation of IIndexUpdateListener

    	public void NotifyAboutIndexUpdate()
    	{
    		throw new NotImplementedException();
    	}

    	#endregion
    }
}
