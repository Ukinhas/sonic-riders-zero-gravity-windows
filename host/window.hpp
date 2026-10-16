#pragma once

#include <functional>

bool open_window(bool fullscreen);
int run_window(const std::function<int()> &emulate);
