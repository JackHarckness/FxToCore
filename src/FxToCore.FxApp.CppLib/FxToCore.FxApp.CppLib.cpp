#include "pch.h"
#include "FxToCore.FxApp.CppLib.h"

CppLib::f2cstring::f2cstring(String^ str)
{
	_str = str;
}

String^ CppLib::f2cstring::Reverse(void)
{
	array<Char>^ buffer = gcnew array<Char>(_str->Length);

	for (int i = 0; i < _str->Length; i++)
	{
		buffer[i] = _str[_str->Length - 1 - i];
	}

	return gcnew String(buffer);
}
