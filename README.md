# Audit.NET #

Audit.NET is a Visual Studio extension that highlights NuGet package dependencies with security vulnerabilities.

Audit.NET relies on the free package and vulnerability database "OSS Index." OSS Index provides open source tools and data for a variety of languages and package managers. Vulnerabilities are drawn from the National Vulnerability Database, a variety of Security Feeds, and community contributions.

Audit.NET scans your dependencies on project load, when new dependencies are added, or when prompted. Vulnerabilities will appear in the Error List, and pertinent lines will be underlined in the packages.config files.

## Installation ##

Audit.NET installation has been tested on Microsoft Visual Studio Community 2017, though it will likely install on earlier versions of Visual Studio Professional.

1. Start Visual Studio
2. Select the "Tools->Extensions and Updates..." menu item
3. The Extensions and Updates dialog will appear
4. In the tree to the left, click "Online"
5. In the tree to the left, wnsure "Visual Studio Gallery" is selected
6. In the search bar to the upper right, type "audit.net" and hit enter
7. The Audit.Net extension should show.
8. Click the "Download" button
9. The "Download and Install" dialog will appear, with the Audit.Net license (BSD 3-clause)
10. Click the install button
11. The dialog will dissapear and the extension will install. A "Restart Now" button will appear at the bottom of Visual Studio. Click it.
12. Visual Studio will restart

## Usage ##

### Startup ###

1. Start Visual Studio on a solution
2. Once the solution has loaded, Audit.NET will automatically run against the solution.
    1. If there are no known vulnerabilities you will see a message in the "Output" tab indicating the number of packages checked.
    2. If there *are* vulnerabilities the "Error List" will be brought to the front indicating the vulnerabilities found.

### New Packages ###

1. Select the "Tools->NuGet Package Manager->Manage NuGet Packages for Solution" menu item
2. The NuGet package manager will open
3. Browser for new packages and install them as appropriate
4. Once installation has completed Audit.NET will run against the new package(s)
    1. If there are no known vulnerabilities you will see a message in the "Output" tab indicating the number of packages checked.
    2. If there *are* vulnerabilities the "Error List" will be brought to the front indicating the vulnerabilities found.

### Running Audit.NET manually ###

1. In the Solution Explorer, select the solution or a project
2. Select the "Project->Audit NuGet Packages" menu item
3. Audit.NET will run against the package(s)
    1. If there are no known vulnerabilities you will see a message in the "Output" tab indicating the number of packages checked.
    2. If there *are* vulnerabilities the "Error List" will be brought to the front indicating the vulnerabilities found.

### Viewing Errors ###

1. Click the "Error List" tab
2. Audit.NET vulnerabilities will appear in the list with the red "X" icon
3. Double click on an error to open the package.config file with the vulnerable package
4. The vulnerable package will be underlined in red
5. Resolve the problem either by using the NuGet package manager, or by hand editing the packages.config
6. If you hand edit the packages.config file you will have to run Audit.NET manually to clear the error

### Viewing More Error Details ###
1. Right click on an error in the errors tab
2. Select "Show Error Help" and the OSS Index page for the selected error will be displayed. This page has additional information such as a list of reference links that can provide evidence of the existence and severity of the vulnerability, as well as possibly insight into the causes, and in some cases possible mitigations.

