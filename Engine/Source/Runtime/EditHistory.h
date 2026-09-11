#pragma once
#include <type_traits>
#include <utility>
#include <vector>
namespace engine {
// Copy any allocating document data before preparation; adoption is then nonthrowing.
template<class T> class EditHistory {
    static_assert(std::is_nothrow_move_constructible_v<T>&&std::is_nothrow_move_assignable_v<T>);
public:
    explicit EditHistory(T initial):value_(std::move(initial)){undo_.reserve(65);redo_.reserve(65);}
    const T& value()const{return value_;}
    template<class Prepare>bool apply(T next,Prepare&& prepare){if(next==value_)return false;T old=value_;prepare(next);push(undo_,std::move(old));redo_.clear();value_=std::move(next);return true;}
    template<class Prepare>bool undo(Prepare&& prepare){if(undo_.empty())return false;T next=undo_.back(),old=value_;prepare(next);push(redo_,std::move(old));undo_.pop_back();value_=std::move(next);return true;}
    template<class Prepare>bool redo(Prepare&& prepare){if(redo_.empty())return false;T next=redo_.back(),old=value_;prepare(next);push(undo_,std::move(old));redo_.pop_back();value_=std::move(next);return true;}
private:
    // Capacity is reserved once; no allocation occurs after a successful preparation.
    static void push(std::vector<T>& history,T value){history.push_back(std::move(value));if(history.size()>64)history.erase(history.begin());}
    T value_;std::vector<T> undo_,redo_;
};
}
