/////////////////////////////////////////READ ME///////////////////////////////////////////

1>This project runs fairly for 2 clients and 2 servers only
2>Dependency Analysis is incomplete.(NO dependency analysis at all)
3>Add the projects to this folder
 "Server_Executive/Projects/"
4>Whenever server is started,it takes port number from command prompt
5>Add server's ports to the XML present in this folder
 "WPF_GUI/ServersAndPorts.xml"
6>Add client ports in the text box obtained after starting the client
7>Type dependencies are obtained on clicking the project name obtained by selecting server.
8>XML file on dependencies is created in this folder
 "WPF_GUI/final_relationship.xml"
9> Relationships are displayed by making LINQ queries into the XML created at line number 372 in Windows.xaml.cs
10>Exceptions are displayed on the server console but server keeps running and does its functionality
11>Please try to re run other test cases, if server displays exception and fails to display required output.
12>If server is not running,the selection made on client does not produce any results(indicates server is off)
13>If run.bat is executed,two servers and two clients gets started------>NOTE:all of them are pointing to same project folder "Server_Executive/Projects/"
If you want to test on different projects for different servers and see whether type table gets updated,try to create another copy of my project and change packages in the newly created "Server_Executive/Projects/" folder