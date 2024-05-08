namespace Clio.Common.Responses;

public record GetPackagesResponse(object errorInfo,
	bool success,
	Packages[] packages);

public record Packages(string createdBy,
	string createdOn,
	string description,
	int hotfixState,
	string id,
	int installBehavior,
	int installType,
	bool isReadOnly,
	string maintainer,
	string modifiedBy,
	string modifiedOn,
	string name,
	int position,
	string repositoryAddress,
	int type,
	string uId,
	string version,
	bool isChanged,
	bool isLocked);