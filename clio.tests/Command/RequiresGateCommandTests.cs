using Clio.Command;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Tests.Command;

[Author("Kirill Krylov", "k.krylov@creatio.com")]
[Category("UnitTests")]
[TestFixture]
public class RequiresGateCommandTests
{


	[Test]
	public void TestCommand(){
		//Arrange
		var sut = new TestCommand();
		var options = new TestCommandOptions();


		//Act
		var result = sut.Execute(options);


		//Assert
		result.Should().Be(0);
	}

}

public class TestCommandOptions : EnvironmentOptions{}
public class TestCommand: Command<TestCommandOptions>
{

	private bool IsGateRequired => true;

	public TestCommand(){
		
	}
	
	public override int Execute(TestCommandOptions options){
		
		if(IsGateRequired) {
			
		}
		
		return 0;
	}

}