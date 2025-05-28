#pragma once

using namespace System;

namespace CppLib
{
	public ref class f2cstring
	{
		String^ _str;

	public:
		f2cstring(String^ str);

		String^ Reverse();
	};
}
