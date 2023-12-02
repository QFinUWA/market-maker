example = "1sadfdsdfsdasdsa"

def solution(input):

    def one_pass(reversed):

        reversed_input = input[::-1] if reversed else input

        letters = ['one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine']
        D = {l[::-1] if reversed else l: str(i) for i, l in enumerate(letters, 1)}

        for i, c in enumerate(reversed_input):
            for d in D:
                if reversed_input.startswith(d, i): 
                    return D[d]

            if c in D.values():
                return c

            
    return one_pass(False) + one_pass(True)

print(solution(example))