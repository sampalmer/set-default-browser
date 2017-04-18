using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UIAutomationClient;

namespace DefaultBrowserChanger
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Launch settings
                var appActiveManager = new ApplicationActivationManager();
                appActiveManager.ActivateApplication(
                    "windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel",
                    "page=SettingsPageAppsDefaults",
                    ActivateOptions.None, //TODO: Find a way to hide the splash screen. Or maybe just hide the window until the splash screen is gone
                    out var pid
                );

                var uiAutomation = new CUIAutomationClass();

                var window = FindAndWait(uiAutomation.GetRootElement(), TreeScope.TreeScope_Children, uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Settings"), TimeSpan.FromSeconds(3));
                try
                {
                    //Can't hide the window since that prevents UI Automation from seeing it
                    ((IUIAutomationWindowPattern)window.GetCurrentPattern(UIA_PatternIds.UIA_WindowPatternId)).SetWindowVisualState(WindowVisualState.WindowVisualState_Minimized);

                    var defaultWebBrowserDropDown = FindAndWait(window, TreeScope.TreeScope_Descendants, uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_AutomationIdPropertyId, "SystemSettings_DefaultApps_Browser_Button"), TimeSpan.FromSeconds(1));
                    ((IUIAutomationInvokePattern)defaultWebBrowserDropDown.GetCurrentPattern(UIA_PatternIds.UIA_InvokePatternId)).Invoke();

                    var chooseAnApplication = FindAndWait(window, TreeScope.TreeScope_Descendants, uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_AutomationIdPropertyId, "DefaultAppsFlyoutPresenter"), TimeSpan.FromSeconds(1));
                    var googleChromeOption = FindAndWait(
                        chooseAnApplication,
                        TreeScope.TreeScope_Descendants,
                        uiAutomation.CreateAndCondition(
                            uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Google Chrome"),
                            uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_ButtonControlTypeId)
                        ),
                        TimeSpan.FromSeconds(1)
                    );
                    ((IUIAutomationInvokePattern)googleChromeOption.GetCurrentPattern(UIA_PatternIds.UIA_InvokePatternId)).Invoke();

                    //If switching from Edge for the first time, it shows a confirmation prompt
                    try
                    {
                        var switchAnyway = FindAndWait(
                            window,
                            TreeScope.TreeScope_Descendants,
                            uiAutomation.CreateAndCondition(
                                uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Switch anyway"),
                                uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_HyperlinkControlTypeId)
                            ),
                            TimeSpan.FromSeconds(1)
                        );
                        ((IUIAutomationInvokePattern)switchAnyway.GetCurrentPattern(UIA_PatternIds.UIA_InvokePatternId)).Invoke();
                    }
                    catch (TimeoutException) { } //All good
                }
                finally
                {
                    ((IUIAutomationWindowPattern)window.GetCurrentPattern(UIA_PatternIds.UIA_WindowPatternId)).Close();
                }
            }
            catch //Ensures all nested finally blocks execute
            {
                throw;
            }
        }

        private static IUIAutomationElement FindAndWait(IUIAutomationElement rootElement, TreeScope scope, IUIAutomationCondition condition, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();

            do
            {
                var result = rootElement.FindAll(scope, condition);
                if (result.Length > 1)
                    throw new Exception("Found multiple elements");
                else if (result.Length == 1)
                    return result.GetElement(0);

                Thread.Sleep(1);
            } while (stopwatch.Elapsed < timeout);

            throw new TimeoutException();
        }
    }

    #region opening panel

    public enum ActivateOptions
	{
		None = 0x00000000,  // No flags set
		DesignMode = 0x00000001,  // The application is being activated for design mode, and thus will not be able to
		// to create an immersive window. Window creation must be done by design tools which
		// load the necessary components by communicating with a designer-specified service on
		// the site chain established on the activation manager.  The splash screen normally
		// shown when an application is activated will also not appear.  Most activations
		// will not use this flag.
		NoErrorUI = 0x00000002,  // Do not show an error dialog if the app fails to activate.                                
		NoSplashScreen = 0x00000004,  // Do not show the splash screen when activating the app.
	}

	[ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IApplicationActivationManager
	{
		IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
	}

	[ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
	class ApplicationActivationManager : IApplicationActivationManager
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)/*, PreserveSig*/]
		public extern IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
	}

    #endregion

    class Win32
    {

        /// <summary>Shows a Window</summary>
        /// <remarks>
        /// <para>To perform certain special effects when showing or hiding a 
        /// window, use AnimateWindow.</para>
        ///<para>The first time an application calls ShowWindow, it should use 
        ///the WinMain function's nCmdShow parameter as its nCmdShow parameter. 
        ///Subsequent calls to ShowWindow must use one of the values in the 
        ///given list, instead of the one specified by the WinMain function's 
        ///nCmdShow parameter.</para>
        ///<para>As noted in the discussion of the nCmdShow parameter, the 
        ///nCmdShow value is ignored in the first call to ShowWindow if the 
        ///program that launched the application specifies startup information 
        ///in the structure. In this case, ShowWindow uses the information 
        ///specified in the STARTUPINFO structure to show the window. On 
        ///subsequent calls, the application must call ShowWindow with nCmdShow 
        ///set to SW_SHOWDEFAULT to use the startup information provided by the 
        ///program that launched the application. This behavior is designed for 
        ///the following situations: </para>
        ///<list type="">
        ///    <item>Applications create their main window by calling CreateWindow 
        ///    with the WS_VISIBLE flag set. </item>
        ///    <item>Applications create their main window by calling CreateWindow 
        ///    with the WS_VISIBLE flag cleared, and later call ShowWindow with the 
        ///    SW_SHOW flag set to make it visible.</item>
        ///</list></remarks>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nCmdShow">Specifies how the window is to be shown. 
        /// This parameter is ignored the first time an application calls 
        /// ShowWindow, if the program that launched the application provides a 
        /// STARTUPINFO structure. Otherwise, the first time ShowWindow is called, 
        /// the value should be the value obtained by the WinMain function in its 
        /// nCmdShow parameter. In subsequent calls, this parameter can be one of 
        /// the WindowShowStyle members.</param>
        /// <returns>
        /// If the window was previously visible, the return value is nonzero. 
        /// If the window was previously hidden, the return value is zero.
        /// </returns>
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        /// <summary>Enumeration of the different ways of showing a window using 
        /// ShowWindow</summary>
        public enum WindowShowStyle : uint
        {
            /// <summary>Hides the window and activates another window.</summary>
            /// <remarks>See SW_HIDE</remarks>
            Hide = 0,
            /// <summary>Activates and displays a window. If the window is minimized 
            /// or maximized, the system restores it to its original size and 
            /// position. An application should specify this flag when displaying 
            /// the window for the first time.</summary>
            /// <remarks>See SW_SHOWNORMAL</remarks>
            ShowNormal = 1,
            /// <summary>Activates the window and displays it as a minimized window.</summary>
            /// <remarks>See SW_SHOWMINIMIZED</remarks>
            ShowMinimized = 2,
            /// <summary>Activates the window and displays it as a maximized window.</summary>
            /// <remarks>See SW_SHOWMAXIMIZED</remarks>
            ShowMaximized = 3,
            /// <summary>Maximizes the specified window.</summary>
            /// <remarks>See SW_MAXIMIZE</remarks>
            Maximize = 3,
            /// <summary>Displays a window in its most recent size and position. 
            /// This value is similar to "ShowNormal", except the window is not 
            /// actived.</summary>
            /// <remarks>See SW_SHOWNOACTIVATE</remarks>
            ShowNormalNoActivate = 4,
            /// <summary>Activates the window and displays it in its current size 
            /// and position.</summary>
            /// <remarks>See SW_SHOW</remarks>
            Show = 5,
            /// <summary>Minimizes the specified window and activates the next 
            /// top-level window in the Z order.</summary>
            /// <remarks>See SW_MINIMIZE</remarks>
            Minimize = 6,
            /// <summary>Displays the window as a minimized window. This value is 
            /// similar to "ShowMinimized", except the window is not activated.</summary>
            /// <remarks>See SW_SHOWMINNOACTIVE</remarks>
            ShowMinNoActivate = 7,
            /// <summary>Displays the window in its current size and position. This 
            /// value is similar to "Show", except the window is not activated.</summary>
            /// <remarks>See SW_SHOWNA</remarks>
            ShowNoActivate = 8,
            /// <summary>Activates and displays the window. If the window is 
            /// minimized or maximized, the system restores it to its original size 
            /// and position. An application should specify this flag when restoring 
            /// a minimized window.</summary>
            /// <remarks>See SW_RESTORE</remarks>
            Restore = 9,
            /// <summary>Sets the show state based on the SW_ value specified in the 
            /// STARTUPINFO structure passed to the CreateProcess function by the 
            /// program that started the application.</summary>
            /// <remarks>See SW_SHOWDEFAULT</remarks>
            ShowDefault = 10,
            /// <summary>Windows 2000/XP: Minimizes a window, even if the thread 
            /// that owns the window is hung. This flag should only be used when 
            /// minimizing windows from a different thread.</summary>
            /// <remarks>See SW_FORCEMINIMIZE</remarks>
            ForceMinimized = 11
        }


        // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);
    }
}
