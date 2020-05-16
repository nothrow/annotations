window.annotation_browser = function(data, navigation, content, search) {

    const cel = function(name, text) {
        const ret = document.createElement(name);

        if (text)
            ret.innerHTML = text;

        return ret;
    }

    const make_depth = function(d) {
        return '&nbsp;'.repeat(d);
    }


    for (const assembly in data) {
        const li = document.createElement('li');
        li.innerText = '🎁' + assembly;
        // li.classList.add('list-group-item');
        navigation.appendChild(li);

        const append_namespace = function(namespace, depth, prepend) {

            for (const key in namespace.Namespaces) {
                const subns = namespace.Namespaces[key];
                console.log(subns);

                const fullName = prepend + '.' + subns.NamespaceName;
                let ndepth = depth;

                if (subns.Types.length > 0) {
                    const li = cel('li');
                    const clickable = cel('a', '📦' + make_depth(depth) + ' ' + fullName);

                    clickable.href = '#!/' + assembly + '/' + fullName;

                    li.appendChild(clickable);

                    navigation.appendChild(li);
                    ++ndepth;
                }

                append_namespace(subns, ndepth, fullName);
            }
        }




        append_namespace(data[assembly].Namespaces, 0, '');


    }
};