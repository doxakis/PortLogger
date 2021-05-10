# PortLogger
A tool to log the traffic between 2 processes and act as an proxy. It can help debug issues with the communication of any process.

# Usage
Let's say you want to forward the port 81 to the host localhost with the port 80 and log everything under the folder temp, you would call:

`PortLogger --in 81 --host localhost --out 80 --destination temp`

# Installation
First, make sure to install the .net core sdk. Then, run the following command in a terminal:

`dotnet tool install -g PortLogger --version 1.0.1`

(To uninstall: `dotnet tool uninstall -g PortLogger`)

# Copyright and license
Code released under the MIT license.
