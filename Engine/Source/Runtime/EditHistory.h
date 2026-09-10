#pragma once
#include <type_traits>
#include <vector>
namespace engine {
// Small value-document history. Preparation must succeed before adopting an edit.
template<class T> class EditHistory {
    static_assert(std::is_nothrow_copy_constructible_v<T>&&std::is_nothrow_copy_assignable_v<T>);
public:
    explicit EditHistory(T initial):value_(initial){undo_.reserve(65);redo_.reserve(65);}
    const T& value()const{return value_;}
    template<class Prepare>bool apply(T next,Prepare&& prepare){if(next==value_)return false;prepare(next);push(undo_,value_);redo_.clear();value_=next;return true;}
    template<class Prepare>bool undo(Prepare&& prepare){if(undo_.empty())return false;const T next=undo_.back();prepare(next);push(redo_,value_);undo_.pop_back();value_=next;return true;}
    template<class Prepare>bool redo(Prepare&& prepare){if(redo_.empty())return false;const T next=redo_.back();prepare(next);push(undo_,value_);redo_.pop_back();value_=next;return true;}
private:
    static void push(std::vector<T>& history,const T& value){history.push_back(value);if(history.size()>64)history.erase(history.begin());}
    T value_;std::vector<T> undo_,redo_;
};
}
