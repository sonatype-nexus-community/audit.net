# Audit.NET #

Audit.NET is a Visual Studio extension that highlights NuGet package dependencies with security vulnerabilities.

Audit.NET relies on the free package and vulnerability database "OSS Index." OSS Index provides open source tools and data for a variety of languages and package managers. Vulnerabilities are drawn from the National Vulnerability Database, a variety of Security Feeds, and community contributions.

Audit.NET scans your dependencies on project load, when new dependencies are added, or when prompted. Vulnerabilities will appear in the Error List, and pertinent lines will be underlined in the packages.config files.

## Installation ##

Audit.NET installation has been tested on Microsoft Visual Studio Community 2015, though it will likely install on earlier versions of Visual Studio Professional.

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

1. Start Visual Studio on a solution
2. Once the solution has loaded, Audit.NET will automatically run against the solution.
    3. If there are no known vulnerabilities you will see a message in the "Output" tab indicating the number of packages checked.
    4. 
