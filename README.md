[![ci.appveyor.com](https://ci.appveyor.com/api/projects/status/github/blushingpenguin/Fluent.Net?branch=master&svg=true)](https://ci.appveyor.com/api/projects/status/github/blushingpenguin/Fluent.Net?branch=master&svg=true)
[![codecov.io](https://codecov.io/gh/blushingpenguin/Fluent.Net/coverage.svg?branch=master)](https://codecov.io/gh/blushingpenguin/Fluent.Net?branch=master)

# Fluent.Net #

Fluent.Net is a C# implementation of Project Fluent, a localization
framework designed to unleash the expressive power of the natural language.

Project Fluent keeps simple things simple and makes complex things possible.
The syntax used for describing translations is easy to read and understand.  At
the same time it allows, when necessary, to represent complex concepts from
natural languages like gender, plurals, conjugations, and others.

A fully working sample project is included [here][(https://github.com/blushingpenguin/Fluent.Net/tree/master/Fluent.Net.SimpleExample).

## Packages ##

Fluent.Net can also be installed from [nuget.org](https://www.nuget.org/packages/Fluent.Net/).

Install with package manager:

    Install-Package Fluent.Net

or with nuget:

    nuget install Fluent.Net

Or with dotnet:

    dotnet add package Fluent.Net

## Learn the FTL syntax ##

FTL is a localization file format used for describing translation resources.
FTL stands for _Fluent Translation List_.

FTL is designed to be simple to read, but at the same time allows to represent
complex concepts from natural languages like gender, plurals, conjugations,
and others.

    hello-user = Hello, { $username }!

[Read the Fluent Syntax Guide][] in order to learn more about the syntax.  If
you're a tool author you may be interested in the formal [EBNF grammar][].

[Read the Fluent Syntax Guide]: http://projectfluent.org/fluent/guide/
[EBNF grammar]: https://github.com/projectfluent/fluent/tree/master/spec

## Discuss ##

We'd love to hear your thoughts on Project Fluent!  Whether you're a localizer looking
for a better way to express yourself in your language, or a developer trying to
make your app localizable and multilingual, or a hacker looking for a project
to contribute to, please do get in touch on the mailing list and the IRC
channel.

- mailing list: [https://lists.mozilla.org/listinfo/tools-l10n](https://lists.mozilla.org/listinfo/tools-l10n)
- IRC channel: [irc://irc.mozilla.org/l20n](irc://irc.mozilla.org/l20n)

## Get Involved ##

Fluent.Net is open-source, licensed under the Apache License, Version 2.0.  We
encourage everyone to take a look at our code and we'll listen to your
feedback.

## Local Development ##

Hacking on `Fluent.Net` is easy! To quickly get started clone the repo:

    git clone https://github.com/blushingpenguin/Fluent.Net.git
    cd Fluent.Net

To compile the code and run the tests just open the solution in
Visual Studio 2017 Community Edition.  To generate a code coverage report
run cover.bat from the solution directory.
