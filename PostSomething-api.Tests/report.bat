echo off

dotnet test --logger "xunit;LogFileName=TestResults.xml" /p:CollectCoverage=true /p:CoverletOutput=TestResults/ /p:CoverletOutputFormat=\"json,opencover,lcov,cobertura\" /p:ThresholdType=line /p:ThresholdStat=total /p:ExcludeByFile=\"**/Program.cs,**/Migrations/*,**/Database/*\" && reportgenerator -targetdir:"./TestResults" -reports:"./TestResults/coverage.info" -reporttypes:Html
