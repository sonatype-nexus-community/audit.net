#region License
// Copyright (c) 2015-2018, Sonatype Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Sonatype, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL SONATYPE BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

//using EnvDTE;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using Microsoft.VisualStudio;

namespace NugetAuditor.VSIX
{
    internal static class VsUtility
    {
		private static readonly HashSet<string> _supportedProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			VsConstants.WebSiteProjectTypeGuid,
			VsConstants.CsharpProjectTypeGuid,
			VsConstants.VbProjectTypeGuid,
			VsConstants.CppProjectTypeGuid,
			VsConstants.JsProjectTypeGuid,
			VsConstants.FsharpProjectTypeGuid,
			VsConstants.NemerleProjectTypeGuid,
			VsConstants.WixProjectTypeGuid,
			VsConstants.SynergexProjectTypeGuid,
			VsConstants.NomadForVisualStudioProjectTypeGuid,
			VsConstants.DxJsProjectTypeGuid
		};

		internal static IVsHierarchy GetHierarchy(this Project project)
        {
            IVsHierarchy hierarchy = null;

            var solution = ServiceLocator.GetInstance<IVsSolution>();

            ErrorHandler.ThrowOnFailure(solution.GetProjectOfUniqueName(project.UniqueName, out hierarchy));

            solution = null;

            return hierarchy;
        }

        internal static string GetPackageReferenceFilePath(this Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projFileLines = File.ReadAllLines(project.FullName);
            foreach(var line in projFileLines)
            {
                if (line.Contains("Project") && line.Contains("\"Microsoft.NET.Sdk") || line.Contains("\'Microsoft.NET.Sdk\'"))
                {
                    return project.FullName;
                }
            }
                            
            var packagesFile = Path.Combine(Path.GetDirectoryName(project.FullName), "packages.config");
            if (File.Exists(packagesFile))
            {
                return packagesFile;
            }
            return string.Empty;
        }

        internal static IVsTextLines GetDocumentTextLines(string path)
        {
            IVsUIHierarchy uiHierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;

            if (VsShellUtilities.IsDocumentOpen(VSAsyncPackage.Instance, path, new Guid(LogicalViewID.TextView), out uiHierarchy, out itemID, out windowFrame))
            {
                IVsTextView pView;
                IVsTextLines textLines;

                pView = VsShellUtilities.GetTextView(windowFrame);

                ErrorHandler.ThrowOnFailure(pView.GetBuffer(out textLines));

                return textLines;
            }

            return null;
        }

        public static Project GetActiveProject(IVsMonitorSelection vsMonitorSelection)
        {
            IntPtr ppHier = IntPtr.Zero;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC = IntPtr.Zero;

            try
            {
                vsMonitorSelection.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC);

                if (ppHier == IntPtr.Zero)
                {
                    return null;
                }

                // multiple items are selected.
                if (pitemid == (uint)VSConstants.VSITEMID.Selection)
                {
                    return null;
                }

                IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(ppHier, typeof(IVsHierarchy)) as IVsHierarchy;
                if (hierarchy != null)
                {
                    object project;
                    if (hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project) >= 0)
                    {
                        return project as Project;
                    }
                }

                return null;
            }
            finally
            {
                if (ppHier != IntPtr.Zero)
                {
                    Marshal.Release(ppHier);
                }
                if (ppSC != IntPtr.Zero)
                {
                    Marshal.Release(ppSC);
                }
            }
        }

        internal static bool IsProjectSupported(this Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

			if (project.Kind != null && _supportedProjectTypes.Contains(project.Kind)) {
				return true;
			}

            // Check if packages.config exists
            //return File.Exists(project.GetPackageReferenceFilePath());

            // IVsPackageInstallerServices.IsPackageInstalled throws InvalidOperationException if project does not support NuGet packages.
            // TODO: Find a better way to detect support for NuGet packages.
            try
            {
                // FIXME: This should not happen
                if (project == null) return false;
                IVsPackageInstallerServices locator = ServiceLocator.GetInstance<IVsPackageInstallerServices>();
                // FIXME: This should not happen
                if (locator == null) return false;
                locator.IsPackageInstalled(project, "__dummy__");
                return true;
            }
			catch (InvalidOperationException)
            {
                return false;
            }
            catch (Exception e)
            {
				ExceptionHelper.WriteToActivityLog(e);
                // FIXME: A variety of project types which do not work with the IsPackageInstalled method will throw exceptions of various sorts.
                // FIXME: Surely there is a better way to check for Nuget support?
                return false;
            }
        }

        internal static bool IsProjectUnloaded(this Project project)
        {
            return project.Kind.Equals(EnvDTE.Constants.vsProjectKindUnmodeled, StringComparison.OrdinalIgnoreCase);
        }

		internal static IEnumerable<Project> GetProjects(this Solution solution)
		{
			if (solution == null || !solution.IsOpen)
			{
				yield break;
			}

			Stack<Project> source = new Stack<Project>();

			foreach (Project project in solution.Projects)
			{
				source.Push(project);
			}

			while (source.Any())
			{
				Project project = source.Pop();

				if (VsUtility.IsProjectUnloaded(project))
				{
					continue;
				}
				
				yield return project;

				try
				{
					var projectItems = project.ProjectItems;

					if (projectItems == null)
					{
						continue;
					}

					foreach (ProjectItem projectItem in projectItems)
					{
						if (projectItem.SubProject != null)
						{
							source.Push(projectItem.SubProject);
						}
					}
				}
				catch (NotImplementedException)
				{
					continue;
				}
			}
		}

		internal static IEnumerable<Project> GetSupportedProjects(this Solution solution)
        {
            if (solution == null || !solution.IsOpen)
            {
                yield break;
            }

            Stack<Project> source = new Stack<Project>();

            foreach (Project project in solution.Projects)
            {
                source.Push(project);
            }

            while (source.Any())
            {
                Project project = source.Pop();

                if (VsUtility.IsProjectUnloaded(project))
                {
                    continue;
                }

                if (VsUtility.IsProjectSupported(project))
                {
                    yield return project;
                }

                try
                {
                    var projectItems = project.ProjectItems;

                    if (projectItems == null)
                    {
                        continue;
                    }

                    foreach (ProjectItem projectItem in projectItems)
                    {
                        if (projectItem.SubProject != null)
                        {
                            source.Push(projectItem.SubProject);
                        }
                    }
                }
                catch (NotImplementedException)
                {
                    continue;
                }
            }
        }

        internal static string GetName(this Solution solution)
        {
            return (string)solution.Properties.Item("Name").Value;
        }

        internal static string GetSolutionName(Solution solution)
        {
            return (string)solution.Properties.Item("Name").Value;
        }
    }
}
